namespace FluxoCaixaDiario.SharedKernel.Events;

public sealed record LancamentoCriadoIntegrationEvent(
    Guid LancamentoId,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    DateTime OcorridoEm);