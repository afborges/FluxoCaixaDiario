using FluxoCaixaDiario.SharedKernel.Domain;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Domain.Events;

public sealed record LancamentoCriadoEvent(
    Guid LancamentoId,
    TipoLancamento Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    DateTime OcorridoEm) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}