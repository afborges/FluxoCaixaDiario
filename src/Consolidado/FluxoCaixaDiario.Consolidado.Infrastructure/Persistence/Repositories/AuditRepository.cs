using FluxoCaixaDiario.Consolidado.Application.Interfaces;
using FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;
using FluxoCaixaDiario.SharedKernel.Audit;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(ConsolidadoDbContext context) : IAuditRepository
{
    public async Task RegistrarAsync(AuditLog auditLog, CancellationToken ct = default)
        => await context.AuditLogs.AddAsync(auditLog, ct);

    public async Task SalvarAlteracoesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}