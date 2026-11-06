using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;

namespace FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentoPorId;

public sealed record ObterLancamentoPorIdQuery(Guid Id) : IRequest<Result<LancamentoDto>>;