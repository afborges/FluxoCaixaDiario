using FluxoCaixaDiario.Consolidado.Domain.Entities;

namespace FluxoCaixaDiario.Consolidado.Domain.Repositories;

public interface IEventoProcessadoRepository
{
    Task<bool> ExisteAsync(Guid lancamentoId, string eventType, CancellationToken ct = default);
    Task AdicionarAsync(EventoProcessado evento, CancellationToken ct = default);
}