using Microsoft.EntityFrameworkCore;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Repositories;

public sealed class ConsolidadoDiarioRepository(ConsolidadoDbContext context)
    : IConsolidadoDiarioRepository
{
    public async Task<ConsolidadoDiario?> ObterPorDataAsync(DateOnly data, CancellationToken ct = default)
        => await context.ConsolidadosDiarios
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Data == data, ct);

    public async Task<IEnumerable<ConsolidadoDiario>> ListarPorPeriodoAsync(
        DateOnly dataInicio, DateOnly dataFim, CancellationToken ct = default)
        => await context.ConsolidadosDiarios
            .AsNoTracking()
            .Where(c => c.Data >= dataInicio && c.Data <= dataFim)
            .OrderBy(c => c.Data)
            .ToListAsync(ct);

    public async Task AdicionarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default)
        => await context.ConsolidadosDiarios.AddAsync(consolidado, ct);

    public Task AtualizarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default)
    {
        context.ConsolidadosDiarios.Update(consolidado);
        return Task.CompletedTask;
    }

    public async Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}