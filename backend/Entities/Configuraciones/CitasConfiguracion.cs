using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

public class CitaConfiguracion : IEntityTypeConfiguration<Cita>
{
    // Nombre de la restricción de exclusión (migración Inicial); el backend la reconoce
    // para traducir la violación 23P01 a un mensaje en español.
    public const string RestriccionSinTraslape = "citas_sin_traslape_dentista";

    public void Configure(EntityTypeBuilder<Cita> e)
    {
        e.ToTable("citas", t =>
        {
            t.HasCheckConstraint("ck_citas_fin_despues_de_inicio", "fin > inicio");
            t.HasCheckConstraint("ck_citas_estado",
                "estado IN ('pendiente','confirmada','completada','cancelada','no_asistio')");
            t.HasCheckConstraint("ck_citas_origen", "origen IN ('web','portal','recepcion')");
            t.HasCheckConstraint("ck_citas_cancelacion_con_motivo",
                "estado <> 'cancelada' OR motivo_cancelacion IS NOT NULL");
            t.HasCheckConstraint("ck_citas_solicitud_cambio_completa",
                "(solicitud_cambio_tipo IS NULL AND solicitud_cambio_motivo IS NULL AND solicitud_cambio_en IS NULL) OR " +
                "(solicitud_cambio_tipo IS NOT NULL AND solicitud_cambio_motivo IS NOT NULL AND solicitud_cambio_en IS NOT NULL)");
            t.HasCheckConstraint("ck_citas_solicitud_cambio_tipo", "solicitud_cambio_tipo IN ('reagendar','cancelar')");
            // "panel": confirmada al crearla en recepción (confirmarAlCrear) o por recepción sin llamada
            t.HasCheckConstraint("ck_citas_confirmacion_canal", "confirmacion_canal IN ('correo','portal','llamada','panel')");
            t.HasCheckConstraint("ck_citas_bloqueo_solo_solicitudes",
                "origen IN ('web','portal') OR bloqueo_expira_en IS NULL");
            t.HasCheckConstraint("ck_citas_bloqueo_despues_de_creacion",
                "bloqueo_expira_en IS NULL OR bloqueo_expira_en > creado_en");
            t.HasCheckConstraint("ck_citas_descuento", "descuento_aplicado >= 0 AND descuento_aplicado <= 100");
            t.HasCheckConstraint("ck_citas_costos", "costo_base >= 0 AND costo_extra >= 0 AND costo_total >= 0");
        });

        e.Property(c => c.Estado).HasMaxLength(20).HasDefaultValue(EstadosCita.Pendiente);
        e.Property(c => c.Origen).HasMaxLength(20);
        e.Property(c => c.Motivo).HasMaxLength(500);
        e.Property(c => c.MotivoCancelacion).HasMaxLength(500);
        e.Property(c => c.MotivoCambio).HasMaxLength(500);
        e.Property(c => c.Version).IsRowVersion();
        e.Property(c => c.CostoBase).HasPrecision(10, 2);
        e.Property(c => c.DescuentoAplicado).HasPrecision(5, 2);
        e.Property(c => c.CostoExtra).HasPrecision(10, 2);
        e.Property(c => c.CostoTotal).HasPrecision(10, 2);
        e.Property(c => c.MotivoExtra).HasMaxLength(300);
        e.Property(c => c.ConfirmacionTokenHash).HasMaxLength(64);
        e.Property(c => c.ConfirmacionCanal).HasMaxLength(20);
        e.Property(c => c.SolicitudCambioTipo).HasMaxLength(20);
        e.Property(c => c.SolicitudCambioMotivo).HasMaxLength(500);
        e.Property(c => c.CreadoEn).HasDefaultValueSql("now()");
        e.Property(c => c.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(c => c.Paciente).WithMany(p => p.Citas).HasForeignKey(c => c.PacienteId);
        e.HasOne(c => c.Dentista).WithMany(d => d.Citas).HasForeignKey(c => c.DentistaId);
        e.HasOne(c => c.Servicio).WithMany(s => s.Citas).HasForeignKey(c => c.ServicioId);
        e.HasOne(c => c.Oferta).WithMany(o => o.Citas).HasForeignKey(c => c.OfertaId);
        e.HasOne(c => c.CreadoPorUsuario).WithMany().HasForeignKey(c => c.CreadoPor);
        e.HasOne(c => c.ConfirmadaPorUsuario).WithMany().HasForeignKey(c => c.ConfirmadaPor);

        e.HasIndex(c => c.Inicio);
        e.HasIndex(c => new { c.DentistaId, c.Inicio });
        e.HasIndex(c => new { c.PacienteId, c.Inicio });
        e.HasIndex(c => new { c.Estado, c.Inicio });
        e.HasIndex(c => c.BloqueoExpiraEn)
            .HasFilter("estado = 'pendiente'")
            .HasDatabaseName("ix_citas_bloqueo_pendiente");
        e.HasIndex(c => c.ConfirmacionTokenHash)
            .IsUnique()
            .HasFilter("confirmacion_token_hash IS NOT NULL");
    }
}

public class CitaHistorialConfiguracion : IEntityTypeConfiguration<CitaHistorial>
{
    public void Configure(EntityTypeBuilder<CitaHistorial> e)
    {
        e.ToTable("cita_historial", t =>
        {
            t.HasCheckConstraint("ck_cita_historial_accion",
                $"accion IN ({string.Join(",", AccionesCitaHistorial.Todas.Select(a => $"'{a}'"))})");
            t.HasCheckConstraint("ck_cita_historial_canal",
                $"canal IS NULL OR canal IN ({string.Join(",", CanalesCitaHistorial.Todos.Select(c => $"'{c}'"))})");
        });

        e.Property(h => h.Accion).HasMaxLength(30);
        e.Property(h => h.Canal).HasMaxLength(20);
        e.Property(h => h.Detalle).HasMaxLength(500);
        e.Property(h => h.Datos).HasColumnType("jsonb");
        e.Property(h => h.CreadoEn).HasDefaultValueSql("now()");

        e.HasOne(h => h.Cita).WithMany(c => c.Historial).HasForeignKey(h => h.CitaId);
        e.HasOne(h => h.Usuario).WithMany().HasForeignKey(h => h.UsuarioId);
        e.HasIndex(h => new { h.CitaId, h.CreadoEn });
    }
}
