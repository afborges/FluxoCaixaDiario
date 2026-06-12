using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Domain.Repositories;

public interface ILancamentoRepository
{
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Lancamento?> ObterPorIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default);
    Task<(IEnumerable<Lancamento> Items, int Total)> ListarAsync(
        DateOnly? dataInicio,
        DateOnly? dataFim,
        TipoLancamento? tipo,
        StatusLancamento? status,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default);
    Task AdicionarAsync(Lancamento lancamento, CancellationToken ct = default);
    Task AtualizarAsync(Lancamento lancamento, CancellationToken ct = default);
    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}