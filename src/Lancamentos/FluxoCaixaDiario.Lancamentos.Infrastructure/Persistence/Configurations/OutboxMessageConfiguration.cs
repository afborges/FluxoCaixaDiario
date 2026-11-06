using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluxoCaixaDiario.SharedKernel.Outbox;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(o => o.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(o => o.CriadoEm).IsRequired();
        builder.Property(o => o.ProcessadoEm);
        builder.Property(o => o.TentativasRetry).IsRequired();

        builder.HasIndex(o => o.ProcessadoEm)
            .HasDatabaseName("IX_OutboxMessages_ProcessadoEm");
    }
}