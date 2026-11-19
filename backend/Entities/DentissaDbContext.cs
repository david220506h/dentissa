using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace backend.Entities;

public partial class DentissaDbContext : DbContext
{
    public DentissaDbContext(DbContextOptions<DentissaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CategoriaServicio> CategoriaServicios { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<Comentario> Comentarios { get; set; }

    public virtual DbSet<ConsultaFoto> ConsultaFotos { get; set; }

    public virtual DbSet<ConsultaRegistro> ConsultaRegistros { get; set; }

    public virtual DbSet<ExamenClinico> ExamenClinicos { get; set; }

    public virtual DbSet<ExamenMedico> ExamenMedicos { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Oferta> Ofertas { get; set; }

    public virtual DbSet<Servicio> Servicios { get; set; }

    public virtual DbSet<ServiciosRealizado> ServiciosRealizados { get; set; }

    public virtual DbSet<TrabajadorServicio> TrabajadorServicios { get; set; }

    public virtual DbSet<Trabajadore> Trabajadores { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoriaServicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC071B6D7B11");

            entity.HasIndex(e => e.Id, "UQ__Categori__3214EC060159CA97").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Citas__3214EC0773750374");

            entity.HasIndex(e => e.Id, "UQ__Citas__3214EC065DE1C2D9").IsUnique();

            entity.Property(e => e.AgendadoPor).HasMaxLength(20);
            entity.Property(e => e.CostoBase).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CostoExtra)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CostoTotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DescuentoAplicado)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("pendiente");
            entity.Property(e => e.MotivoExtra).HasMaxLength(300);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__ClienteId__2DE6D218");

            entity.HasOne(d => d.Oferta).WithMany(p => p.Cita)
                .HasForeignKey(d => d.OfertaId)
                .HasConstraintName("FK__Citas__OfertaId__30C33EC3");

            entity.HasOne(d => d.Servicio).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__ServicioI__2FCF1A8A");

            entity.HasOne(d => d.Trabajador).WithMany(p => p.Cita)
                .HasForeignKey(d => d.TrabajadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Citas__Trabajado__2EDAF651");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clientes__3214EC0742437D8F");

            entity.HasIndex(e => e.Id, "UQ__Clientes__3214EC062BFF1DB8").IsUnique();

            entity.Property(e => e.Curp)
                .HasMaxLength(18)
                .HasColumnName("CURP");
            entity.Property(e => e.Sexo).HasMaxLength(10);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Clientes__Usuari__282DF8C2");
        });

        modelBuilder.Entity<Comentario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Comentar__3214EC075D474137");

            entity.HasIndex(e => e.Id, "UQ__Comentar__3214EC065870F6E7").IsUnique();

            entity.Property(e => e.Comentario1)
                .HasMaxLength(1000)
                .HasColumnName("Comentario");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.Visible).HasDefaultValue(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Comentarios)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comentari__Clien__3587F3E0");

            entity.HasOne(d => d.ServicioRealizado).WithMany(p => p.Comentarios)
                .HasForeignKey(d => d.ServicioRealizadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comentari__Servi__367C1819");
        });

        modelBuilder.Entity<ConsultaFoto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Consulta__3214EC07DEB6691B");

            entity.HasIndex(e => e.Id, "UQ__Consulta__3214EC0632329033").IsUnique();

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.FotoUrl).HasMaxLength(500);

            entity.HasOne(d => d.ConsultaRegistro).WithMany(p => p.ConsultaFotos)
                .HasForeignKey(d => d.ConsultaRegistroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ConsultaF__Consu__32AB8735");
        });

        modelBuilder.Entity<ConsultaRegistro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Consulta__3214EC07D81F5AE2");

            entity.HasIndex(e => e.Id, "UQ__Consulta__3214EC0632583E5B").IsUnique();

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.Observaciones).HasMaxLength(1000);

            entity.HasOne(d => d.Cita).WithMany(p => p.ConsultaRegistros)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ConsultaR__CitaI__31B762FC");
        });

        modelBuilder.Entity<ExamenClinico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ExamenCl__3214EC0724005E21");

            entity.ToTable("ExamenClinico");

            entity.HasIndex(e => e.Id, "UQ__ExamenCl__3214EC067853F36A").IsUnique();

            entity.Property(e => e.ExamenExtraoral).HasMaxLength(500);
            entity.Property(e => e.ExamenIntraoral).HasMaxLength(500);
            entity.Property(e => e.FechaActualizacion)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Pa)
                .HasMaxLength(20)
                .HasColumnName("PA");
            entity.Property(e => e.ResultadoExamenesAuxiliares).HasMaxLength(500);
            entity.Property(e => e.Temperatura).HasColumnType("decimal(4, 1)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ExamenClinicos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ExamenCli__Clien__3A4CA8FD");
        });

        modelBuilder.Entity<ExamenMedico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ExamenMe__3214EC07A6105AA8");

            entity.ToTable("ExamenMedico");

            entity.HasIndex(e => e.Id, "UQ__ExamenMe__3214EC063D2B2734").IsUnique();

            entity.Property(e => e.Alergias).HasDefaultValue(false);
            entity.Property(e => e.AlergiasDetalle).HasMaxLength(300);
            entity.Property(e => e.AntecedenteFamiliar).HasMaxLength(500);
            entity.Property(e => e.Asma).HasDefaultValue(false);
            entity.Property(e => e.AsmaDetalle).HasMaxLength(300);
            entity.Property(e => e.ComentarioAdicional).HasMaxLength(500);
            entity.Property(e => e.Diabetes).HasDefaultValue(false);
            entity.Property(e => e.DiabetesDetalle).HasMaxLength(300);
            entity.Property(e => e.EnfermedadesSanguineas).HasDefaultValue(false);
            entity.Property(e => e.EnfermedadesSanguineasDetalle).HasMaxLength(300);
            entity.Property(e => e.FechaActualizacion)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.Fuma).HasDefaultValue(false);
            entity.Property(e => e.FumaDetalle).HasMaxLength(300);
            entity.Property(e => e.Gastritis).HasDefaultValue(false);
            entity.Property(e => e.GastritisDetalle).HasMaxLength(300);
            entity.Property(e => e.HemorragiasExtraccion).HasDefaultValue(false);
            entity.Property(e => e.Hepatitis).HasDefaultValue(false);
            entity.Property(e => e.HepatitisDetalle).HasMaxLength(300);
            entity.Property(e => e.MedicacionDetalle).HasMaxLength(300);
            entity.Property(e => e.MedicacionPermanente).HasDefaultValue(false);
            entity.Property(e => e.MotivoConsulta).HasMaxLength(500);
            entity.Property(e => e.OperacionGrande).HasDefaultValue(false);
            entity.Property(e => e.OperacionGrandeDetalle).HasMaxLength(300);
            entity.Property(e => e.OtraEnfermedad).HasDefaultValue(false);
            entity.Property(e => e.OtraEnfermedadDetalle).HasMaxLength(300);
            entity.Property(e => e.OtrasMolestias).HasMaxLength(300);
            entity.Property(e => e.PresionAlta).HasDefaultValue(false);
            entity.Property(e => e.PresionAltaDetalle).HasMaxLength(300);
            entity.Property(e => e.PresionBaja).HasDefaultValue(false);
            entity.Property(e => e.PresionBajaDetalle).HasMaxLength(300);
            entity.Property(e => e.ProblemasCardiacos).HasDefaultValue(false);
            entity.Property(e => e.ProblemasCardiacosDetalle).HasMaxLength(300);
            entity.Property(e => e.RechinamientoDientes).HasDefaultValue(false);
            entity.Property(e => e.SangraEncias).HasDefaultValue(false);
            entity.Property(e => e.Ulceras).HasDefaultValue(false);
            entity.Property(e => e.UlcerasDetalle).HasMaxLength(300);
            entity.Property(e => e.Vih)
                .HasDefaultValue(false)
                .HasColumnName("VIH");
            entity.Property(e => e.Vihdetalle)
                .HasMaxLength(300)
                .HasColumnName("VIHDetalle");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ExamenMedicos)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ExamenMed__Clien__395884C4");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC078E91E719");

            entity.HasIndex(e => e.Id, "UQ__Notifica__3214EC065D5044C1").IsUnique();

            entity.Property(e => e.Enviada).HasDefaultValue(false);
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Cita).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificac__CitaI__37703C52");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificac__Clien__3864608B");
        });

        modelBuilder.Entity<Oferta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ofertas__3214EC078B8B1982");

            entity.HasIndex(e => e.Id, "UQ__Ofertas__3214EC06CFD305F9").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descuento).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Servicio).WithMany(p => p.Oferta)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ofertas__Servici__2CF2ADDF");
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Servicio__3214EC075ED99012");

            entity.HasIndex(e => e.Id, "UQ__Servicio__3214EC069D208174").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Disponible).HasDefaultValue(true);
            entity.Property(e => e.Imagen).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Servicios)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Servicios__Categ__2A164134");
        });

        modelBuilder.Entity<ServiciosRealizado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Servicio__3214EC07B7D57154");

            entity.HasIndex(e => e.Id, "UQ__Servicio__3214EC06DF21AC7B").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.FechaRealizacion)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.FotoAntes).HasMaxLength(500);
            entity.Property(e => e.FotoDespues).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(200);

            entity.HasOne(d => d.Servicio).WithMany(p => p.ServiciosRealizados)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Servicios__Servi__339FAB6E");

            entity.HasOne(d => d.Trabajador).WithMany(p => p.ServiciosRealizados)
                .HasForeignKey(d => d.TrabajadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Servicios__Traba__3493CFA7");
        });

        modelBuilder.Entity<TrabajadorServicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Trabajad__3214EC07CA0FCC1F");

            entity.HasIndex(e => e.Id, "UQ__Trabajad__3214EC06F073EBC8").IsUnique();

            entity.HasOne(d => d.Servicio).WithMany(p => p.TrabajadorServicios)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Trabajado__Servi__2BFE89A6");

            entity.HasOne(d => d.Trabajador).WithMany(p => p.TrabajadorServicios)
                .HasForeignKey(d => d.TrabajadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Trabajado__Traba__2B0A656D");
        });

        modelBuilder.Entity<Trabajadore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Trabajad__3214EC0703360127");

            entity.HasIndex(e => e.Id, "UQ__Trabajad__3214EC066CEE7209").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Especialidad).HasMaxLength(100);
            entity.Property(e => e.Foto).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(100);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Trabajadores)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Trabajado__Usuar__29221CFB");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC077B925824");

            entity.HasIndex(e => e.Id, "UQ__Usuarios__3214EC06D3EC7A7A").IsUnique();

            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.Rol).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
