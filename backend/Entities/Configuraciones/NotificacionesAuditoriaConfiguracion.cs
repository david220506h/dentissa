using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

public class NotificacionConfiguracion : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> e)
    {
        e.ToTable("notificaciones", t =>
        {
            t.HasCheckConstraint("ck_notificaciones_destinatario", "num_nonnulls(usuario_id, paciente_id) = 1");
            t.HasCheckConstraint("ck_notificaciones_canal", "canal IN ('interna','correo')");
            t.HasCheckConstraint("ck_notificaciones_tipo",
                "tipo IN ('solicitud_nueva','confirmacion','recordatorio','cambio','cancelacion','solicitud_cambio')");
        });

        e.Property(n => n.Tipo).HasMaxLength(30);
        e.Property(n => n.Canal).HasMaxLength(20);
        e.Property(n => n.Asunto).HasMaxLength(200);
        e.Property(n => n.Mensaje).HasMaxLength(2000);
        e.Property(n => n.ErrorEnvio).HasMaxLength(500);
        e.Property(n => n.Leida).HasDefaultValue(false);
        e.Property(n => n.Enviada).HasDefaultValue(false);
        e.Property(n => n.CreadoEn).HasDefaultValueSql("now()");

        e.HasOne(n => n.Usuario).WithMany().HasForeignKey(n => n.UsuarioId);
        e.HasOne(n => n.Paciente).WithMany(p => p.Notificaciones).HasForeignKey(n => n.PacienteId);
        e.HasOne(n => n.Cita).WithMany(c => c.Notificaciones).HasForeignKey(n => n.CitaId);
    }
}

public class AuditoriaConfiguracion : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> e)
    {
        e.ToTable("auditoria");

        e.Property(a => a.OcurridoEn).HasDefaultValueSql("now()");
        e.Property(a => a.UsuarioCorreo).HasColumnType("citext");
        e.Property(a => a.Accion).HasMaxLength(40);
        e.Property(a => a.Entidad).HasMaxLength(60);
        e.Property(a => a.EntidadId).HasMaxLength(60);
        e.Property(a => a.Detalle).HasColumnType("jsonb");
        e.Property(a => a.Ip).HasMaxLength(45);

        e.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId);
        e.HasIndex(a => a.OcurridoEn);
        e.HasIndex(a => new { a.Entidad, a.EntidadId });
    }
}
