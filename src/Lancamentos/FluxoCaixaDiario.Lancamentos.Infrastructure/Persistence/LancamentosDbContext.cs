using Microsoft.EntityFrameworkCore;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.SharedKernel.Audit;
using FluxoCaixaDiario.SharedKernel.Outbox;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;

public sealed class LancamentosDbContext(DbContextOptions<LancamentosDbContext> options)
    : DbContext(options)
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LancamentosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}