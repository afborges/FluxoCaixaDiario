using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Consolidado.Application.DTOs;

namespace FluxoCaixaDiario.Consolidado.Application.Queries.ObterHistorico;

public sealed record ObterHistoricoConsolidadoQuery(
    DateOnly DataInicio,
    DateOnly DataFim) : IRequest<Result<IEnumerable<ConsolidadoDiarioDto>>>;