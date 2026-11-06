namespace FluxoCaixaDiario.SharedKernel.Audit;

public sealed class AuditLog
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Servico { get; private init; } = string.Empty;
    public string Operacao { get; private init; } = string.Empty;
    public string? UsuarioOuChave { get; private init; }
    public string? CorrelationId { get; private init; }
    public string? DadosEntrada { get; private init; }
    public string? DadosSaida { get; private init; }
    public bool Sucesso { get; private init; }
    public string? MensagemErro { get; private init; }
    public DateTime OcorridoEm { get; private init; } = DateTime.UtcNow;
    public long DuracaoMs { get; private init; }

    private AuditLog() { }

    public static AuditLog Criar(
        string servico,
        string operacao,
        bool sucesso,
        long duracaoMs,
        string? usuarioOuChave = null,
        string? correlationId = null,
        string? dadosEntrada = null,
        string? dadosSaida = null,
        string? mensagemErro = null) => new()
    {
        Servico = servico,
        Operacao = operacao,
        Sucesso = sucesso,
        DuracaoMs = duracaoMs,
        UsuarioOuChave = usuarioOuChave,
        CorrelationId = correlationId,
        DadosEntrada = dadosEntrada,
        DadosSaida = dadosSaida,
        MensagemErro = mensagemErro
    };
}