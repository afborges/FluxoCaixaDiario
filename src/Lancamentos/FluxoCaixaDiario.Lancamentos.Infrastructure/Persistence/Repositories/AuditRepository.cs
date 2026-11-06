using FluxoCaixaDiario.Lancamentos.Application.Interfaces;
using FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;
using FluxoCaixaDiario.SharedKernel.Audit;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(LancamentosDbContext context) : IAuditRepository
{
    public async Task RegistrarAsync(AuditLog auditLog, CancellationToken ct = default)
        => await context.AuditLogs.AddAsync(auditLog, ct);

    public async Task SalvarAlteracoesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}