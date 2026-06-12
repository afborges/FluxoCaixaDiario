namespace FluxoCaixaDiario.SharedKernel.Events;

public sealed record LancamentoCanceladoIntegrationEvent(
    Guid LancamentoId,
    string Tipo,
    decimal Valor,
    DateOnly Data,
    DateTime OcorridoEm);