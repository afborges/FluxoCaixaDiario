using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Application.DTOs;

public sealed record LancamentoDto(
    Guid Id,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    string Status,
    DateTime CriadoEm,
    DateTime? CanceladoEm);