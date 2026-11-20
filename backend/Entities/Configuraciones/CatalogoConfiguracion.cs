using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Entities.Configuraciones;

public class CategoriaServicioConfiguracion : IEntityTypeConfiguration<CategoriaServicio>
{
    public void Configure(EntityTypeBuilder<CategoriaServicio> e)
    {
        e.ToTable("categorias_servicio");

        e.Property(c => c.Nombre).HasMaxLength(100);
        e.Property(c => c.Descripcion).HasMaxLength(300);
        e.Property(c => c.Activo).HasDefaultValue(true).HasSentinel(true);
        e.Property(c => c.CreadoEn).HasDefaultValueSql("now()");
        e.Property(c => c.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasIndex(c => c.Nombre).IsUnique();
    }
}

public class ServicioConfiguracion : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> e)
    {
        e.ToTable("servicios", t =>
        {
            t.HasCheckConstraint("ck_servicios_precio", "precio >= 0");
            t.HasCheckConstraint("ck_servicios_duracion_slots", "duracion_slots BETWEEN 1 AND 4");
        });

        e.Property(s => s.Nombre).HasMaxLength(100);
        e.Property(s => s.Descripcion).HasMaxLength(500);
        e.Property(s => s.Precio).HasPrecision(10, 2);
        e.Property(s => s.DuracionSlots).HasDefaultValue((short)1).HasSentinel((short)0);
        e.Property(s => s.Imagen).HasMaxLength(500);
        e.Property(s => s.Activo).HasDefaultValue(true).HasSentinel(true);
        e.Property(s => s.CreadoEn).HasDefaultValueSql("now()");
        e.Property(s => s.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(s => s.Categoria).WithMany(c => c.Servicios).HasForeignKey(s => s.CategoriaId);
    }
}

public class OfertaConfiguracion : IEntityTypeConfiguration<Oferta>
{
    public void Configure(EntityTypeBuilder<Oferta> e)
    {
        e.ToTable("ofertas", t =>
        {
            t.HasCheckConstraint("ck_ofertas_descuento", "descuento > 0 AND descuento <= 100");
            t.HasCheckConstraint("ck_ofertas_vigencia", "fecha_fin >= fecha_inicio");
        });

        e.Property(o => o.Descuento).HasPrecision(5, 2);
        e.Property(o => o.Activo).HasDefaultValue(true).HasSentinel(true);
        e.Property(o => o.CreadoEn).HasDefaultValueSql("now()");
        e.Property(o => o.ActualizadoEn).HasDefaultValueSql("now()");

        e.HasOne(o => o.Servicio).WithMany(s => s.Ofertas).HasForeignKey(o => o.ServicioId);
        e.HasIndex(o => new { o.ServicioId, o.FechaInicio, o.FechaFin });
    }
}

public class ServiciosRealizadoConfiguracion : IEntityTypeConfiguration<ServiciosRealizado>
{
    public void Configure(EntityTypeBuilder<ServiciosRealizado> e)
    {
        // Galería de casos del sitio (no es expediente)
        e.ToTable("servicios_realizados");

        e.Property(s => s.Titulo).HasMaxLength(200);
        e.Property(s => s.Descripcion).HasMaxLength(1000);
        e.Property(s => s.FotoAntes).HasMaxLength(500);
        e.Property(s => s.FotoDespues).HasMaxLength(500);
        e.Property(s => s.FechaRealizacion).HasDefaultValueSql("now()");

        e.HasOne(s => s.Servicio).WithMany(s => s.ServiciosRealizados).HasForeignKey(s => s.ServicioId);
        e.HasOne(s => s.Dentista).WithMany(d => d.ServiciosRealizados).HasForeignKey(s => s.DentistaId);
    }
}
