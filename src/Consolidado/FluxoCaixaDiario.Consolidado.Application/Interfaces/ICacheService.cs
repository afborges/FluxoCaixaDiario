namespace FluxoCaixaDiario.Consolidado.Application.Interfaces;

public interface ICacheService
{
    Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default) where T : class;
    Task DefinirAsync<T>(string chave, T valor, TimeSpan ttl, CancellationToken ct = default) where T : class;
    Task RemoverAsync(string chave, CancellationToken ct = default);
}