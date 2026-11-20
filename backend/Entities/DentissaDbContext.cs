using Microsoft.EntityFrameworkCore;

namespace backend.Entities;

public class DentissaDbContext : DbContext
{
    public DentissaDbContext(DbContextOptions<DentissaDbContext> options)
        : base(options)
    {
    }

    // Cuentas y personas
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Dentista> Dentistas => Set<Dentista>();
    public DbSet<DentistaServicio> DentistaServicios => Set<DentistaServicio>();

    // Catálogo
    public DbSet<CategoriaServicio> CategoriasServicio => Set<CategoriaServicio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Oferta> Ofertas => Set<Oferta>();
    public DbSet<ServiciosRealizado> ServiciosRealizados => Set<ServiciosRealizado>();

    // Agenda
    public DbSet<ConfiguracionAgenda> ConfiguracionAgenda => Set<ConfiguracionAgenda>();
    public DbSet<HorarioDia> HorarioDias => Set<HorarioDia>();
    public DbSet<HorarioDescanso> HorarioDescansos => Set<HorarioDescanso>();
    public DbSet<AgendaCierre> AgendaCierres => Set<AgendaCierre>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<CitaHistorial> CitaHistorial => Set<CitaHistorial>();

    // Notificaciones y auditoría
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Auditoria> Auditoria => Set<Auditoria>();

    // Expediente (sin rediseño en esta fase)
    public DbSet<ExamenMedico> ExamenesMedicos => Set<ExamenMedico>();
    public DbSet<ExamenClinico> ExamenesClinicos => Set<ExamenClinico>();
    public DbSet<ConsultaRegistro> ConsultaRegistros => Set<ConsultaRegistro>();
    public DbSet<ConsultaFoto> ConsultaFotos => Set<ConsultaFoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DentissaDbContext).Assembly);

        // Nada se borra en cascada: lo clínico y lo financiero no se borra físicamente
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AplicarMarcasDeTiempo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AplicarMarcasDeTiempo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // creado_en y actualizado_en los llena el contexto, no cada controlador
    private void AplicarMarcasDeTiempo()
    {
        var ahora = DateTime.UtcNow;
        foreach (var entrada in ChangeTracker.Entries<IConMarcasDeTiempo>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreadoEn = ahora;
                entrada.Entity.ActualizadoEn = ahora;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Property(e => e.CreadoEn).IsModified = false;
                entrada.Entity.ActualizadoEn = ahora;
            }
        }
    }
}
