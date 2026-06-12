using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluxoCaixaDiario.Consolidado.Domain.Entities;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Configurations;

public sealed class ConsolidadoDiarioConfiguration : IEntityTypeConfiguration<ConsolidadoDiario>
{
    public void Configure(EntityTypeBuilder<ConsolidadoDiario> builder)
    {
        builder.ToTable("ConsolidadoDiario");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Data)
            .HasColumnType("date")
            .IsRequired();

        builder.HasIndex(c => c.Data)
            .IsUnique()
            .HasDatabaseName("IX_ConsolidadoDiario_Data");

        builder.Property(c => c.TotalCreditos)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.TotalDebitos)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.QuantidadeLancamentos).IsRequired();
        builder.Property(c => c.UltimaAtualizacao).IsRequired();

        builder.Ignore(c => c.SaldoFinal);
        builder.Ignore(c => c.DomainEvents);
    }
}