using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

public class CitaTokenConfiguracion : IEntityTypeConfiguration<CitaToken>
{
    public void Configure(EntityTypeBuilder<CitaToken> e)
    {
        e.ToTable("cita_tokens", t =>
        {
            t.HasCheckConstraint("ck_cita_tokens_proposito", "proposito IN ('confirmar','gestionar')");
            t.HasCheckConstraint("ck_cita_tokens_hash", "token_hash ~ '^[0-9a-f]{64}$'");
        });

        e.Property(t => t.Proposito).HasMaxLength(20);
        e.Property(t => t.TokenHash).HasColumnType("char(64)");
        e.HasIndex(t => t.TokenHash).IsUnique();
        e.HasIndex(t => new { t.CitaId, t.Proposito });

        e.HasOne(t => t.Cita).WithMany().HasForeignKey(t => t.CitaId).OnDelete(DeleteBehavior.Restrict);
    }
}
