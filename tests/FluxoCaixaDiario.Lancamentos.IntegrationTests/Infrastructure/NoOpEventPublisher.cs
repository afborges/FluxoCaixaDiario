using FluxoCaixaDiario.Lancamentos.Application.Interfaces;

namespace FluxoCaixaDiario.Lancamentos.IntegrationTests.Infrastructure;

internal sealed class NoOpEventPublisher : IEventPublisher
{
    public Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
