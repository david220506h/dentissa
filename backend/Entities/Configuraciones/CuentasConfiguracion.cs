using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

// Nombres de tablas y columnas en snake_case (UseSnakeCaseNamingConvention).
// Lo que EF Core no puede expresar (columna rol generada en dentistas y pacientes,
// FK compuesta que amarra el rol, exclusión de traslapes, triggers) está en la migración Inicial.

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> e)
    {
        e.ToTable("usuarios", t =>
        {
            t.HasCheckConstraint("ck_usuarios_rol", "rol IN ('admin','asistente','dentista','paciente')");
        });

        e.Property(u => u.Correo).HasColumnType("citext");
        e.Property(u => u.PasswordHash).HasMaxLength(255);
        e.Property(u => u.Rol).HasMaxLength(20);
        e.Property(u => u.Nombre).HasMaxLength(100);
        e.Property(u => u.Apellido).HasMaxLength(100);
        e.Property(u => u.Telefono).HasMaxLength(20);
        e.Property(u => u.RefreshToken).HasMaxLength(500);
        e.Property(u => u.Activo).HasDefaultValue(true).HasSentinel(true);
        e.Property(u => u.CreadoEn).HasDefaultValueSql("now()");
        e.Property(u => u.ActualizadoEn).HasDefaultValueSql("now()");

        // Correo único sin distinguir mayúsculas (citext) entre cuentas no eliminadas
        e.HasIndex(u => u.Correo)
            .IsUnique()
            .HasFilter("eliminado_en IS NULL")
            .HasDatabaseName("ux_usuarios_correo_vigente");

        // Destino de las FK compuestas (usuario_id, rol) de dentistas y pacientes
        e.HasIndex(u => new { u.Id, u.Rol })
            .IsUnique()
            .HasDatabaseName("ux_usuarios_id_rol");

        e.HasIndex(u => u.RefreshToken);
    }
}

public class PacienteConfiguracion : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> e)
    {
        e.ToTable("pacientes", t =>
        {
            t.HasCheckConstraint("ck_pacientes_contacto", "correo IS NOT NULL OR telefono IS NOT NULL");
            t.HasCheckConstraint("ck_pacientes_origen", "origen IN ('web','portal','recepcion')");
            t.HasCheckConstraint("ck_pacientes_sexo", "sexo IN ('femenino','masculino','otro','no_especifica')");
            t.HasCheckConstraint("ck_pacientes_tipo_sangre", "tipo_sangre IN ('A+','A-','B+','B-','AB+','AB-','O+','O-')");
        });

        e.Property(p => p.Nombre).HasMaxLength(100);
        e.Property(p => p.Apellido).HasMaxLength(100);
        e.Property(p => p.Correo).HasColumnType("citext");
        e.Property(p => p.Telefono).HasMaxLength(20);
        e.Property(p => p.Sexo).HasMaxLength(20);
        e.Property(p => p.Curp).HasMaxLength(18);
        e.Property(p => p.Direccion).HasMaxLength(300);
        e.Property(p => p.TipoSangre).HasMaxLength(3);
        e.Property(p => p.Origen).HasMaxLength(20);
        e.Property(p => p.ContactoVerificado).HasDefaultValue(false);
        e.Property(p => p.CreadoEn).HasDefaultValueSql("now()");
        e.Property(p => p.ActualizadoEn).HasDefaultValueSql("now()");

        // Una cuenta tiene a lo más un paciente; el invitado no tiene cuenta (NULL)
        e.HasOne(p => p.Usuario)
            .WithOne(u => u.Paciente)
            .HasForeignKey<Paciente>(p => p.UsuarioId);

        e.HasIndex(p => p.Correo);
        e.HasIndex(p => p.Telefono);
    }
}

public class DentistaConfiguracion : IEntityTypeConfiguration<Dentista>
{
    public void Configure(EntityTypeBuilder<Dentista> e)
    {
        e.ToTable("dentistas");

        e.Property(d => d.CedulaProfesional).HasMaxLength(20);
        e.Property(d => d.Especialidad).HasMaxLength(100);
        e.Property(d => d.Titulo).HasMaxLength(100);
        e.Property(d => d.Descripcion).HasMaxLength(500);
        e.Property(d => d.Foto).HasMaxLength(500);
        e.Property(d => d.Activo).HasDefaultValue(true).HasSentinel(true);
        e.Property(d => d.CreadoEn).HasDefaultValueSql("now()");
        e.Property(d => d.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(d => d.Usuario)
            .WithOne(u => u.Dentista)
            .HasForeignKey<Dentista>(d => d.UsuarioId);
    }
}

public class DentistaServicioConfiguracion : IEntityTypeConfiguration<DentistaServicio>
{
    public void Configure(EntityTypeBuilder<DentistaServicio> e)
    {
        e.ToTable("dentista_servicios");
        e.HasKey(ds => new { ds.DentistaId, ds.ServicioId });

        e.HasOne(ds => ds.Dentista).WithMany(d => d.DentistaServicios).HasForeignKey(ds => ds.DentistaId);
        e.HasOne(ds => ds.Servicio).WithMany(s => s.DentistaServicios).HasForeignKey(ds => ds.ServicioId);
    }
}
