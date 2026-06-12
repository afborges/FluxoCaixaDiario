using Microsoft.EntityFrameworkCore;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Repositories;

public sealed class LancamentoRepository(LancamentosDbContext context) : ILancamentoRepository
{
    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Lancamento?> ObterPorIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default)
        => await context.Lancamentos.FirstOrDefaultAsync(l => l.IdempotencyKey == idempotencyKey, ct);

    public async Task<(IEnumerable<Lancamento> Items, int Total)> ListarAsync(
        DateOnly? dataInicio,
        DateOnly? dataFim,
        TipoLancamento? tipo,
        StatusLancamento? status,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var query = context.Lancamentos.AsNoTracking().AsQueryable();

        if (dataInicio.HasValue)
            query = query.Where(l => l.Data >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(l => l.Data <= dataFim.Value);

        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken ct = default)
        => await context.Lancamentos.AddAsync(lancamento, ct);

    public Task AtualizarAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        context.Lancamentos.Update(lancamento);
        return Task.CompletedTask;
    }

    public async Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}