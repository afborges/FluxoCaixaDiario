namespace FluxoCaixaDiario.SharedKernel.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string EventType { get; private init; } = string.Empty;
    public string Payload { get; private init; } = string.Empty;
    public DateTime CriadoEm { get; private init; } = DateTime.UtcNow;
    public DateTime? ProcessadoEm { get; private set; }
    public int TentativasRetry { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Criar(string eventType, string payload) => new()
    {
        EventType = eventType,
        Payload = payload
    };

    public void MarcarComoProcessado() => ProcessadoEm = DateTime.UtcNow;

    public void IncrementarTentativa() => TentativasRetry++;
}