using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;

namespace FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentoPorId;

public sealed class ObterLancamentoPorIdQueryHandler(ILancamentoRepository repository)
    : IRequestHandler<ObterLancamentoPorIdQuery, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        ObterLancamentoPorIdQuery query, CancellationToken ct)
    {
        var lancamento = await repository.ObterPorIdAsync(query.Id, ct);
        if (lancamento is null)
            return Result.Failure<LancamentoDto>($"Lançamento {query.Id} não encontrado.");

        return Result.Success(new LancamentoDto(
            lancamento.Id,
            lancamento.Tipo.ToString(),
            lancamento.Valor.Quantia,
            lancamento.Descricao,
            lancamento.Data,
            lancamento.Status.ToString(),
            lancamento.CriadoEm,
            lancamento.CanceladoEm));
    }
}