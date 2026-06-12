namespace FluxoCaixaDiario.Consolidado.Application.Interfaces;

public interface IAuditContextAccessor
{
    string? CorrelationId { get; }
    string? UsuarioOuChave { get; }
}