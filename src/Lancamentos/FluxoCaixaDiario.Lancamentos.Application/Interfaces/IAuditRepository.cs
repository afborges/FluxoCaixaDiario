using FluxoCaixaDiario.SharedKernel.Audit;

namespace FluxoCaixaDiario.Lancamentos.Application.Interfaces;

public interface IAuditRepository
{
    Task RegistrarAsync(AuditLog auditLog, CancellationToken ct = default);
    Task SalvarAlteracoesAsync(CancellationToken ct = default);
}