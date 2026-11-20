using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

// Tablas del expediente: se migran sin rediseño (columnas clínicas iguales).
// Un trigger de la migración impide su borrado físico (RN-11).

public class ExamenMedicoConfiguracion : IEntityTypeConfiguration<ExamenMedico>
{
    public void Configure(EntityTypeBuilder<ExamenMedico> e)
    {
        e.ToTable("examen_medico");

        e.Property(x => x.MotivoConsulta).HasMaxLength(500);
        e.Property(x => x.AntecedenteFamiliar).HasMaxLength(500);
        e.Property(x => x.ComentarioAdicional).HasMaxLength(500);
        e.Property(x => x.PresionAltaDetalle).HasMaxLength(300);
        e.Property(x => x.PresionBajaDetalle).HasMaxLength(300);
        e.Property(x => x.Vihdetalle).HasMaxLength(300);
        e.Property(x => x.DiabetesDetalle).HasMaxLength(300);
        e.Property(x => x.HepatitisDetalle).HasMaxLength(300);
        e.Property(x => x.AsmaDetalle).HasMaxLength(300);
        e.Property(x => x.GastritisDetalle).HasMaxLength(300);
        e.Property(x => x.UlcerasDetalle).HasMaxLength(300);
        e.Property(x => x.FumaDetalle).HasMaxLength(300);
        e.Property(x => x.EnfermedadesSanguineasDetalle).HasMaxLength(300);
        e.Property(x => x.ProblemasCardiacosDetalle).HasMaxLength(300);
        e.Property(x => x.OtraEnfermedadDetalle).HasMaxLength(300);
        e.Property(x => x.AlergiasDetalle).HasMaxLength(300);
        e.Property(x => x.OperacionGrandeDetalle).HasMaxLength(300);
        e.Property(x => x.MedicacionDetalle).HasMaxLength(300);
        e.Property(x => x.OtrasMolestias).HasMaxLength(300);
        e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
        e.Property(x => x.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(x => x.Paciente).WithMany(p => p.ExamenesMedicos).HasForeignKey(x => x.PacienteId);
    }
}

public class ExamenClinicoConfiguracion : IEntityTypeConfiguration<ExamenClinico>
{
    public void Configure(EntityTypeBuilder<ExamenClinico> e)
    {
        e.ToTable("examen_clinico");

        e.Property(x => x.Pa).HasMaxLength(20);
        e.Property(x => x.Temperatura).HasPrecision(4, 1);
        e.Property(x => x.ExamenExtraoral).HasMaxLength(500);
        e.Property(x => x.ExamenIntraoral).HasMaxLength(500);
        e.Property(x => x.ResultadoExamenesAuxiliares).HasMaxLength(500);
        e.Property(x => x.Observaciones).HasMaxLength(500);
        e.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
        e.Property(x => x.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(x => x.Paciente).WithMany(p => p.ExamenesClinicos).HasForeignKey(x => x.PacienteId);
        e.HasOne(x => x.Cita).WithMany(c => c.ExamenesClinicos).HasForeignKey(x => x.CitaId);
    }
}

public class ConsultaRegistroConfiguracion : IEntityTypeConfiguration<ConsultaRegistro>
{
    public void Configure(EntityTypeBuilder<ConsultaRegistro> e)
    {
        e.ToTable("consulta_registros");

        e.Property(x => x.Observaciones).HasMaxLength(1000);
        e.Property(x => x.Fecha).HasDefaultValueSql("now()");

        e.HasOne(x => x.Cita).WithMany(c => c.ConsultaRegistros).HasForeignKey(x => x.CitaId);
    }
}

public class ConsultaFotoConfiguracion : IEntityTypeConfiguration<ConsultaFoto>
{
    public void Configure(EntityTypeBuilder<ConsultaFoto> e)
    {
        e.ToTable("consulta_fotos");

        e.Property(x => x.FotoUrl).HasMaxLength(500);
        e.Property(x => x.Fecha).HasDefaultValueSql("now()");

        e.HasOne(x => x.ConsultaRegistro).WithMany(r => r.ConsultaFotos).HasForeignKey(x => x.ConsultaRegistroId);
    }
}
