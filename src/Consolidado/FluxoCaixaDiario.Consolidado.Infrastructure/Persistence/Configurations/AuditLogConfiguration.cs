using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluxoCaixaDiario.SharedKernel.Audit;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Servico)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Operacao)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.UsuarioOuChave)
            .HasMaxLength(100);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);

        builder.Property(a => a.DadosEntrada)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.DadosSaida)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.Sucesso).IsRequired();

        builder.Property(a => a.MensagemErro)
            .HasMaxLength(2000);

        builder.Property(a => a.OcorridoEm).IsRequired();
        builder.Property(a => a.DuracaoMs).IsRequired();

        builder.HasIndex(a => a.OcorridoEm)
            .HasDatabaseName("IX_AuditLogs_OcorridoEm");
        builder.HasIndex(a => a.Operacao)
            .HasDatabaseName("IX_AuditLogs_Operacao");
        builder.HasIndex(a => a.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");
    }
}