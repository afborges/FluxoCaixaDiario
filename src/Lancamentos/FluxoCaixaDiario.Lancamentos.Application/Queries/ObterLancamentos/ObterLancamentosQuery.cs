using MediatR;
using FluxoCaixaDiario.SharedKernel.Result;
using FluxoCaixaDiario.Lancamentos.Application.DTOs;

namespace FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentos;

public sealed record ObterLancamentosQuery(
    DateOnly? DataInicio,
    DateOnly? DataFim,
    string? Tipo,
    string? Status,
    int Pagina = 1,
    int TamanhoPagina = 20) : IRequest<Result<PaginadoDto<LancamentoDto>>>;