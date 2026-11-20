using backend.Constants;
using Microsoft.EntityFrameworkCore;

namespace backend.Entities;

public class DentissaDbContext : DbContext
{
    public const string FiltroBorradoLogico = "BorradoLogico";

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

        // Borrado lógico: filtro con nombre para que las consultas de citas puedan
        // ignorar solo este filtro (IgnoreQueryFilters([FiltroBorradoLogico]))
        modelBuilder.Entity<Usuario>().HasQueryFilter(FiltroBorradoLogico, e => e.EliminadoEn == null);
        modelBuilder.Entity<Paciente>().HasQueryFilter(FiltroBorradoLogico, e => e.EliminadoEn == null);
        modelBuilder.Entity<Dentista>().HasQueryFilter(FiltroBorradoLogico, e => e.EliminadoEn == null);
        modelBuilder.Entity<Servicio>().HasQueryFilter(FiltroBorradoLogico, e => e.EliminadoEn == null);
        modelBuilder.Entity<Oferta>().HasQueryFilter(FiltroBorradoLogico, e => e.EliminadoEn == null);

        // Nada se borra en cascada: lo clínico y lo financiero no se borra físicamente.
        // Hacia entidades con borrado lógico la FK es ClientNoAction: un Remove() del
        // principal (que se guarda como borrado lógico) no toca a sus dependientes en
        // memoria, y la base sigue impidiendo el borrado físico (NO ACTION).
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = typeof(IEliminable).IsAssignableFrom(fk.PrincipalEntityType.ClrType)
                ? DeleteBehavior.ClientNoAction
                : DeleteBehavior.Restrict;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConvertirBorradosEnLogicos();
        var admins = AdminsQueDejanDeSerlo();
        if (admins.Count > 0 && !Usuarios.Any(u => u.Rol == Roles.Admin && u.Activo && !admins.Contains(u.Id)))
            throw new UltimoAdministradorException();
        AplicarMarcasDeTiempo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ConvertirBorradosEnLogicos();
        var admins = AdminsQueDejanDeSerlo();
        if (admins.Count > 0 && !await Usuarios.AnyAsync(u => u.Rol == Roles.Admin && u.Activo && !admins.Contains(u.Id), cancellationToken))
            throw new UltimoAdministradorException();
        AplicarMarcasDeTiempo();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Administradores activos que en este guardado se desactivan, se eliminan
    // (lógicamente) o cambian de rol. Si no queda otro admin activo, no se guarda.
    private List<int> AdminsQueDejanDeSerlo()
        => ChangeTracker.Entries<Usuario>()
            .Where(e => e.State == EntityState.Modified)
            .Where(e => (string?)e.OriginalValues[nameof(Usuario.Rol)] == Roles.Admin
                        && (bool)e.OriginalValues[nameof(Usuario.Activo)]!
                        && e.OriginalValues[nameof(Usuario.EliminadoEn)] == null)
            .Where(e => e.Entity.Rol != Roles.Admin || !e.Entity.Activo || e.Entity.EliminadoEn != null)
            .Select(e => e.Entity.Id)
            .ToList();

    // Un Remove() sobre una entidad con borrado lógico se guarda como
    // UPDATE … SET eliminado_en = now(): la fila nunca sale de la tabla
    private void ConvertirBorradosEnLogicos()
    {
        var ahora = DateTime.UtcNow;
        foreach (var entrada in ChangeTracker.Entries<IEliminable>().Where(e => e.State == EntityState.Deleted))
        {
            entrada.State = EntityState.Modified;
            foreach (var propiedad in entrada.Properties)
                propiedad.IsModified = false;
            entrada.Entity.EliminadoEn = ahora;
            entrada.Property(e => e.EliminadoEn).IsModified = true;
        }
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
