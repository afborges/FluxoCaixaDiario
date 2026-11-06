using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluxoCaixaDiario.Lancamentos.Domain.Entities;
using FluxoCaixaDiario.Lancamentos.Domain.Enums;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("Lancamentos");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.IdempotencyKey);

        builder.Property(l => l.Tipo)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.OwnsOne(l => l.Valor, v =>
        {
            v.Property(x => x.Quantia)
                .HasColumnName("Valor")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.Property(l => l.Descricao)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(l => l.Data)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(l => l.CriadoEm).IsRequired();
        builder.Property(l => l.CanceladoEm);
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedAt);
        builder.Property(l => l.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(l => l.Data).HasDatabaseName("IX_Lancamentos_Data");
        builder.HasIndex(l => l.Status).HasDatabaseName("IX_Lancamentos_Status");
        builder.HasIndex(l => l.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_Lancamentos_IdempotencyKey");

        builder.Ignore(l => l.DomainEvents);
    }
}