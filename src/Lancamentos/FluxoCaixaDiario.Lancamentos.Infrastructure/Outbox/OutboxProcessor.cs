using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;
using FluxoCaixaDiario.SharedKernel.Events;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan _intervalo = TimeSpan.FromSeconds(2);
    private const int MaxTentativas = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor iniciado.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarMensagensPendentesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Erro no ciclo do OutboxProcessor.");
            }
            await Task.Delay(_intervalo, stoppingToken);
        }
    }

    private async Task ProcessarMensagensPendentesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pendentes = await context.OutboxMessages
            .Where(m => m.ProcessadoEm == null && m.TentativasRetry < MaxTentativas)
            .OrderBy(m => m.CriadoEm)
            .Take(50)
            .ToListAsync(ct);

        if (pendentes.Count == 0)
            return;

        foreach (var mensagem in pendentes)
        {
            try
            {
                await PublicarMensagemAsync(publisher, mensagem, ct);
                mensagem.MarcarComoProcessado();
                logger.LogInformation(
                    "OutboxMessage {Id} publicada: {EventType}", mensagem.Id, mensagem.EventType);
            }
            catch (Exception ex)
            {
                mensagem.IncrementarTentativa();
                logger.LogWarning(ex,
                    "Falha ao publicar OutboxMessage {Id} (tentativa {N}): {EventType}",
                    mensagem.Id, mensagem.TentativasRetry, mensagem.EventType);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private static async Task PublicarMensagemAsync(
        IEventPublisher publisher, SharedKernel.Outbox.OutboxMessage mensagem, CancellationToken ct)
    {
        var criado = typeof(LancamentoCriadoIntegrationEvent).FullName;
        var cancelado = typeof(LancamentoCanceladoIntegrationEvent).FullName;

        if (mensagem.EventType == criado)
        {
            var evento = JsonSerializer.Deserialize<LancamentoCriadoIntegrationEvent>(
                mensagem.Payload, _jsonOptions)!;
            await publisher.PublicarAsync(evento, ct);
        }
        else if (mensagem.EventType == cancelado)
        {
            var evento = JsonSerializer.Deserialize<LancamentoCanceladoIntegrationEvent>(
                mensagem.Payload, _jsonOptions)!;
            await publisher.PublicarAsync(evento, ct);
        }
        else
        {
            throw new InvalidOperationException(
                $"EventType desconhecido no Outbox: {mensagem.EventType}");
        }
    }
}