using MassTransit;
using Microsoft.Extensions.Logging;
using FluxoCaixaDiario.Lancamentos.Application.Interfaces;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Messaging;

public sealed class LancamentoEventPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<LancamentoEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class
    {
        await publishEndpoint.Publish(evento, ct);
        logger.LogInformation("Evento publicado: {EventType}", typeof(T).Name);
    }
}