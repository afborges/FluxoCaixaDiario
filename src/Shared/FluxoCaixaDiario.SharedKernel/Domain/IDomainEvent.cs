namespace FluxoCaixaDiario.SharedKernel.Domain;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OcorridoEm { get; }
}