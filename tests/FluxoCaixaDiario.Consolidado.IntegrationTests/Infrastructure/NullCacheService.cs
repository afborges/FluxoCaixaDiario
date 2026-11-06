using FluxoCaixaDiario.Consolidado.Application.Interfaces;

namespace FluxoCaixaDiario.Consolidado.IntegrationTests.Infrastructure;

internal sealed class NullCacheService : ICacheService
{
    public Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task DefinirAsync<T>(string chave, T valor, TimeSpan ttl, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task RemoverAsync(string chave, CancellationToken ct = default)
        => Task.CompletedTask;
}
