using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

public class ConfiguracionAgendaConfiguracion : IEntityTypeConfiguration<ConfiguracionAgenda>
{
    public void Configure(EntityTypeBuilder<ConfiguracionAgenda> e)
    {
        e.ToTable("configuracion_agenda", t =>
        {
            t.HasCheckConstraint("ck_configuracion_agenda_unica", "id = 1");
            t.HasCheckConstraint("ck_configuracion_agenda_valores",
                "duracion_slot_minutos > 0 AND anticipacion_recepcion_minutos >= 0 " +
                "AND liberacion_antes_cita_atencion_minutos > 0 AND vida_maxima_solicitud_atencion_minutos > 0 " +
                "AND horizonte_reserva_dias > 0");
            // Toda solicitud en línea tiene al menos (anticipación − liberación) de atención para confirmarse
            t.HasCheckConstraint("ck_configuracion_agenda_anticipacion_mayor_que_liberacion",
                "anticipacion_en_linea_atencion_minutos > liberacion_antes_cita_atencion_minutos");
        });

        e.Property(c => c.Id).ValueGeneratedNever();
        e.Property(c => c.ActualizadoEn).HasDefaultValueSql("now()");
        e.Property(c => c.AnticipacionEnLineaAtencionMinutos).HasDefaultValue((short)180);
        e.Property(c => c.AnticipacionRecepcionMinutos).HasDefaultValue((short)60);
        e.Property(c => c.LiberacionAntesCitaAtencionMinutos).HasDefaultValue((short)120);
        e.Property(c => c.VidaMaximaSolicitudAtencionMinutos).HasDefaultValue((short)720);
        e.Property(c => c.HorizonteReservaDias).HasDefaultValue((short)60);

        e.HasOne(c => c.ActualizadoPorUsuario).WithMany().HasForeignKey(c => c.ActualizadoPor);
    }
}

public class HorarioDiaConfiguracion : IEntityTypeConfiguration<HorarioDia>
{
    public void Configure(EntityTypeBuilder<HorarioDia> e)
    {
        e.ToTable("horario_dias", t =>
        {
            t.HasCheckConstraint("ck_horario_dias_dia_semana", "dia_semana BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_horario_dias_horas",
                "NOT laborable OR (hora_apertura IS NOT NULL AND hora_cierre IS NOT NULL AND hora_cierre > hora_apertura)");
        });

        e.HasOne(d => d.Configuracion).WithMany(c => c.Dias).HasForeignKey(d => d.ConfiguracionId);
        e.HasIndex(d => new { d.ConfiguracionId, d.DiaSemana }).IsUnique();
    }
}

public class HorarioDescansoConfiguracion : IEntityTypeConfiguration<HorarioDescanso>
{
    public void Configure(EntityTypeBuilder<HorarioDescanso> e)
    {
        e.ToTable("horario_descansos", t =>
        {
            t.HasCheckConstraint("ck_horario_descansos_dia_semana", "dia_semana IS NULL OR dia_semana BETWEEN 1 AND 7");
            t.HasCheckConstraint("ck_horario_descansos_horas", "hora_fin > hora_inicio");
        });

        e.HasOne(d => d.Configuracion).WithMany(c => c.Descansos).HasForeignKey(d => d.ConfiguracionId);
    }
}

public class AgendaCierreConfiguracion : IEntityTypeConfiguration<AgendaCierre>
{
    public void Configure(EntityTypeBuilder<AgendaCierre> e)
    {
        e.ToTable("agenda_cierres", t =>
        {
            t.HasCheckConstraint("ck_agenda_cierres_horas",
                "(hora_inicio IS NULL AND hora_fin IS NULL) OR " +
                "(hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
        });

        e.Property(c => c.Motivo).HasMaxLength(200);
        e.Property(c => c.CreadoEn).HasDefaultValueSql("now()");

        e.HasOne(c => c.Configuracion).WithMany(c => c.Cierres).HasForeignKey(c => c.ConfiguracionId);
        e.HasOne(c => c.CreadoPorUsuario).WithMany().HasForeignKey(c => c.CreadoPor);
        e.HasIndex(c => c.Fecha);
    }
}
