using FluxoCaixaDiario.Consolidado.Domain.Entities;

namespace FluxoCaixaDiario.Consolidado.Domain.Repositories;

public interface IConsolidadoDiarioRepository
{
    Task<ConsolidadoDiario?> ObterPorDataAsync(DateOnly data, CancellationToken ct = default);
    Task<IEnumerable<ConsolidadoDiario>> ListarPorPeriodoAsync(
        DateOnly dataInicio, DateOnly dataFim, CancellationToken ct = default);
    Task AdicionarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default);
    Task AtualizarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default);
    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}