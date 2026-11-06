namespace FluxoCaixaDiario.Consolidado.Domain.Entities;

public sealed class EventoProcessado
{
    public Guid LancamentoId { get; private init; }
    public string EventType { get; private init; } = string.Empty;
    public DateTime ProcessadoEm { get; private init; }

    private EventoProcessado() { }

    public static EventoProcessado Criar(Guid lancamentoId, string eventType) => new()
    {
        LancamentoId = lancamentoId,
        EventType = eventType,
        ProcessadoEm = DateTime.UtcNow
    };
}