namespace FluxoCaixaDiario.Lancamentos.Application.Interfaces;

public interface IOutboxRepository
{
    Task EnfileirarAsync<T>(T evento, CancellationToken ct = default) where T : class;
}