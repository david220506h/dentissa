using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace backend.Entities;

// Solo para las herramientas de EF Core (dotnet ef migrations add): permite generar
// migraciones sin secretos ni arrancar la aplicación. No abre conexión.
// Para aplicar migraciones se pasa la cadena real con --connection o con la variable
// de entorno ConnectionStrings__DefaultConnection.
public class DentissaDbContextFactory : IDesignTimeDbContextFactory<DentissaDbContext>
{
    public DentissaDbContext CreateDbContext(string[] args)
    {
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                     ?? "Host=localhost;Database=dentissa_diseno";

        var opciones = new DbContextOptionsBuilder<DentissaDbContext>()
            .UseNpgsql(cadena)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DentissaDbContext(opciones);
    }
}
