using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands.CancelarLancamento;

public sealed record CancelarLancamentoCommand(Guid Id) : IRequest<Result>;