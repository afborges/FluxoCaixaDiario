using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FluxoCaixaDiario.Consolidado.Domain.Entities;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence.Configurations;

public sealed class EventoProcessadoConfiguration : IEntityTypeConfiguration<EventoProcessado>
{
    public void Configure(EntityTypeBuilder<EventoProcessado> builder)
    {
        builder.ToTable("EventosProcessados");
        builder.HasKey(e => new { e.LancamentoId, e.EventType });

        builder.Property(e => e.EventType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.ProcessadoEm).IsRequired();
    }
}