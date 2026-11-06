using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Consolidado.Application.DTOs;
using FluxoCaixaDiario.Consolidado.Domain.Repositories;

namespace FluxoCaixaDiario.Consolidado.Application.Queries.ObterHistorico;

public sealed class ObterHistoricoConsolidadoQueryHandler(IConsolidadoDiarioRepository repository)
    : IRequestHandler<ObterHistoricoConsolidadoQuery, Result<IEnumerable<ConsolidadoDiarioDto>>>
{
    public async Task<Result<IEnumerable<ConsolidadoDiarioDto>>> Handle(
        ObterHistoricoConsolidadoQuery query, CancellationToken ct)
    {
        var items = await repository.ListarPorPeriodoAsync(query.DataInicio, query.DataFim, ct);

        var dtos = items.Select(c => new ConsolidadoDiarioDto(
            c.Data, c.TotalCreditos, c.TotalDebitos,
            c.SaldoFinal, c.QuantidadeLancamentos, c.UltimaAtualizacao));

        return Result.Success(dtos);
    }
}