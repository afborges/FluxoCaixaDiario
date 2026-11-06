using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using FluxoCaixaDiario.SharedKernel.Events;
using FluxoCaixaDiario.SharedKernel.Audit;
using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Application.Metrics;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Messaging;

public sealed class LancamentoCanceladoConsumer(
    IConsolidadoDiarioRepository repository,
    IEventoProcessadoRepository eventosProcessados,
    ICacheService cache,
    IAuditRepository auditRepository,
    ConsolidadoMetrics metrics,
    ILogger<LancamentoCanceladoConsumer> logger)
    : IConsumer<LancamentoCanceladoIntegrationEvent>
{
    private const string EventType = nameof(LancamentoCanceladoIntegrationEvent);

    public async Task Consume(ConsumeContext<LancamentoCanceladoIntegrationEvent> context)
    {
        var evento = context.Message;
        var ct = context.CancellationToken;
        var sw = Stopwatch.StartNew();
        var usuarioOuChave = context.Headers.Get<string>("X-Api-Key") ?? "system";
        var correlationId = context.CorrelationId?.ToString() ?? Guid.NewGuid().ToString();

        if (await eventosProcessados.ExisteAsync(evento.LancamentoId, EventType, ct))
        {
            logger.LogWarning(
                "Evento duplicado ignorado: {LancamentoId} ({EventType})",
                evento.LancamentoId, EventType);
            return;
        }

        logger.LogInformation(
            "Processando LancamentoCanceladoEvent: {LancamentoId} | Data: {Data}",
            evento.LancamentoId, evento.Data);

        var consolidado = await repository.ObterPorDataAsync(evento.Data, ct);
        if (consolidado is null)
        {
            logger.LogWarning(
                "Consolidado não encontrado para a data {Data} ao processar cancelamento {LancamentoId}",
                evento.Data, evento.LancamentoId);
            sw.Stop();
            await RegistrarAuditAsync(evento.LancamentoId, evento.Data, false, sw.ElapsedMilliseconds,
                usuarioOuChave, correlationId, $"Consolidado não encontrado para {evento.Data:yyyy-MM-dd}", ct);
            return;
        }

        consolidado.ReverterLancamento(evento.Tipo, evento.Valor);
        await repository.AtualizarAsync(consolidado, ct);

        await eventosProcessados.AdicionarAsync(
            EventoProcessado.Criar(evento.LancamentoId, EventType), ct);

        await repository.SalvarAlteracoesAsync(ct);

        var cacheKey = $"consolidado:{evento.Data:yyyy-MM-dd}";
        await cache.RemoverAsync(cacheKey, ct);

        sw.Stop();

        logger.LogInformation(
            "Consolidado do dia {Data} revertido. Novo saldo: {Saldo}",
            evento.Data, consolidado.SaldoFinal);

        await RegistrarAuditAsync(evento.LancamentoId, evento.Data, true, sw.ElapsedMilliseconds,
            usuarioOuChave, correlationId, null, ct);

        metrics.ConsolidacoesProcessadas.Add(1, new KeyValuePair<string, object?>("tipo", "cancelado"));
    }

    private async Task RegistrarAuditAsync(
        Guid lancamentoId, DateOnly data, bool sucesso, long duracaoMs,
        string usuarioOuChave, string correlationId, string? mensagemErro, CancellationToken ct)
    {
        try
        {
            var auditLog = AuditLog.Criar(
                servico: "Consolidado",
                operacao: "ReverterLancamentoCancelado",
                sucesso: sucesso,
                duracaoMs: duracaoMs,
                usuarioOuChave: usuarioOuChave,
                correlationId: correlationId,
                dadosEntrada: $"LancamentoId={lancamentoId};Data={data:yyyy-MM-dd}",
                mensagemErro: mensagemErro);
            await auditRepository.RegistrarAsync(auditLog, ct);
            await auditRepository.SalvarAlteracoesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao persistir audit log de ReverterLancamentoCancelado {LancamentoId}", lancamentoId);
        }
    }
}