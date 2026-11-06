using Microsoft.EntityFrameworkCore;
using FluxoCaixaDiario.Consolidado.Domain.Entities;
using FluxoCaixaDiario.SharedKernel.Audit;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;

public sealed class ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options)
    : DbContext(options)
{
    public DbSet<ConsolidadoDiario> ConsolidadosDiarios => Set<ConsolidadoDiario>();
    public DbSet<EventoProcessado> EventosProcessados => Set<EventoProcessado>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidadoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}