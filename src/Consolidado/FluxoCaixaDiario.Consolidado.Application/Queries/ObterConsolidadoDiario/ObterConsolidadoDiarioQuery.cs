using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Consolidado.Application.DTOs;

namespace FluxoCaixaDiario.Consolidado.Application.Queries.ObterConsolidadoDiario;

public sealed record ObterConsolidadoDiarioQuery(DateOnly Data)
    : IRequest<Result<ConsolidadoDiarioDto>>;