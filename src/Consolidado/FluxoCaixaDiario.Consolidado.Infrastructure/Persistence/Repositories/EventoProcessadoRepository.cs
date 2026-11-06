using Microsoft.EntityFrameworkCore;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Repositories;

public sealed class EventoProcessadoRepository(ConsolidadoDbContext context)
    : IEventoProcessadoRepository
{
    public async Task<bool> ExisteAsync(Guid lancamentoId, string eventType, CancellationToken ct = default)
        => await context.EventosProcessados
            .AnyAsync(e => e.LancamentoId == lancamentoId && e.EventType == eventType, ct);

    public async Task AdicionarAsync(EventoProcessado evento, CancellationToken ct = default)
        => await context.EventosProcessados.AddAsync(evento, ct);
}