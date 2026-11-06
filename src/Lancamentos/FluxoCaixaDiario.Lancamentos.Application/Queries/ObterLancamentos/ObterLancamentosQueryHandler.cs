using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;
using FluxoCaixaDiario.Lancamentos.Domain.Repositories;

namespace FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentos;

public sealed class ObterLancamentosQueryHandler(ILancamentoRepository repository)
    : IRequestHandler<ObterLancamentosQuery, Result<PaginadoDto<LancamentoDto>>>
{
    public async Task<Result<PaginadoDto<LancamentoDto>>> Handle(
        ObterLancamentosQuery query, CancellationToken ct)
    {
        TipoLancamento? tipo = query.Tipo is not null && Enum.TryParse<TipoLancamento>(query.Tipo, out var t) ? t : null;
        StatusLancamento? status = query.Status is not null && Enum.TryParse<StatusLancamento>(query.Status, out var s) ? s : null;

        var (items, total) = await repository.ListarAsync(
            query.DataInicio, query.DataFim, tipo, status,
            query.Pagina, query.TamanhoPagina, ct);

        var dtos = items.Select(l => new LancamentoDto(
            l.Id, l.Tipo.ToString(), l.Valor.Quantia, l.Descricao,
            l.Data, l.Status.ToString(), l.CriadoEm, l.CanceladoEm));

        return Result.Success(new PaginadoDto<LancamentoDto>(dtos, total, query.Pagina, query.TamanhoPagina));
    }
}