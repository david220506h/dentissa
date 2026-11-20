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
                "duracion_slot_minutos > 0 AND anticipacion_minima_horas >= 0 " +
                "AND bloqueo_pendiente_horas > 0 AND liberacion_antes_cita_horas >= 0");
        });

        e.Property(c => c.Id).ValueGeneratedNever();
        e.Property(c => c.ActualizadoEn).HasDefaultValueSql("now()");

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
