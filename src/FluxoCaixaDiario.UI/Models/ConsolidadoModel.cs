namespace FluxoCaixaDiario.UI.Models;

public record ConsolidadoModel(
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal SaldoFinal,
    int QuantidadeLancamentos,
    DateTime UltimaAtualizacao);
