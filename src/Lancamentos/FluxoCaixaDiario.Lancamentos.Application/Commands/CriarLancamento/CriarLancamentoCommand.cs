using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;

namespace FluxoCaixaDiario.Lancamentos.Application.Commands.CriarLancamento;

public sealed record CriarLancamentoCommand(
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    Guid? IdempotencyKey = null) : IRequest<Result<LancamentoDto>>;