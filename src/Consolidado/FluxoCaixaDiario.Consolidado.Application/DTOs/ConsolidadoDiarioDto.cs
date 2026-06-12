namespace FluxoCaixaDiario.Consolidado.Application.DTOs;

public sealed record ConsolidadoDiarioDto(
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal SaldoFinal,
    int QuantidadeLancamentos,
    DateTime UltimaAtualizacao);