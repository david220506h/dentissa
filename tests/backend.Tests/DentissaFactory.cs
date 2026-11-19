using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using backend.Constants;
using backend.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

// Levanta la API completa en memoria. Solo se sustituye el DbContext por EF InMemory;
// el proveedor de la aplicación (SQL Server) no cambia.
public class DentissaFactory : WebApplicationFactory<Program>
{
    public const string PasswordPruebas = "Prueba123!";

    private readonly string _baseDatos = Guid.NewGuid().ToString();
    private readonly string _carpetaImagenes = Path.Combine(Path.GetTempPath(), "dentissa-tests", Guid.NewGuid().ToString("N"));

    // Límites altos por defecto para que las pruebas no choquen con el rate limiter
    protected virtual int LimiteLogin => 1000;

    // Clave aleatoria por ejecución: no hay secretos en el código de pruebas
    protected virtual string? ClaveJwt => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public DentissaFactory()
    {
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=no-se-usa-en-pruebas");
        builder.UseSetting("JwtSettings:SecretKey", ClaveJwt ?? "");
        builder.UseSetting("RateLimiting:Login:PorIpYCorreo:PermitLimit", LimiteLogin.ToString());
        builder.UseSetting("RateLimiting:Login:PorIp:PermitLimit", (LimiteLogin * 6).ToString());
        builder.UseSetting("RateLimiting:Registro:PorIp:PermitLimit", LimiteLogin.ToString());
        builder.UseSetting("CatalogoImagenes:Ruta", _carpetaImagenes);

        builder.ConfigureServices(services =>
        {
            var registrosEf = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<DentissaDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DentissaDbContext) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<DentissaDbContext>)).ToList();
            foreach (var d in registrosEf) services.Remove(d);

            services.AddDbContext<DentissaDbContext>(o => o.UseInMemoryDatabase(_baseDatos));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_carpetaImagenes))
            Directory.Delete(_carpetaImagenes, recursive: true);
    }

    public async Task<T> ConDbAsync<T>(Func<DentissaDbContext, Task<T>> accion)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
        return await accion(db);
    }

    public Task<Usuario> CrearUsuarioAsync(string rol, string? email = null) => ConDbAsync(async db =>
    {
        var usuario = new Usuario
        {
            Nombre = "Prueba",
            Apellido = rol,
            Email = email ?? $"{rol}-{Guid.NewGuid():N}@prueba.test",
            Password = BCrypt.Net.BCrypt.HashPassword(PasswordPruebas),
            Rol = rol,
            FechaRegistro = DateTime.Now,
            // Simula un refresh token guardado para comprobar que nunca se expone
            RefreshToken = "refresh-de-prueba-" + Guid.NewGuid().ToString("N"),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(1)
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    });

    // Cliente HTTP con sesión iniciada mediante el login real de la API
    public async Task<HttpClient> ClienteConRolAsync(string rol)
    {
        var usuario = await CrearUsuarioAsync(rol);
        var client = CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email = usuario.Email, password = PasswordPruebas });
        res.EnsureSuccessStatusCode();
        var login = await res.Content.ReadFromJsonAsync<LoginRespuesta>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    // Cita completa (cliente, trabajador, servicio) para probar las respuestas de citas
    public Task<int> CrearCitaAsync() => ConDbAsync(async db =>
    {
        var paciente = await CrearUsuarioAsync(Roles.Cliente);
        var dentista = await CrearUsuarioAsync(Roles.Secretaria);

        var cliente = new Cliente { UsuarioId = paciente.Id, Curp = "XEXX010101HNEXXXA4" };
        var trabajador = new Trabajadore { UsuarioId = dentista.Id, Especialidad = "General" };
        var categoria = new CategoriaServicio { Nombre = "General" };
        var servicio = new Servicio { Nombre = "Limpieza", Precio = 500, Categoria = categoria, Disponible = true };
        db.AddRange(cliente, trabajador, categoria, servicio);
        await db.SaveChangesAsync();

        var cita = new Cita
        {
            ClienteId = cliente.Id,
            TrabajadorId = trabajador.Id,
            ServicioId = servicio.Id,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Hora = new TimeOnly(9, 0),
            Estado = "pendiente",
            CostoBase = 500,
            CostoTotal = 500
        };
        db.Citas.Add(cita);
        await db.SaveChangesAsync();
        return cita.Id;
    });

    public record LoginRespuesta(string AccessToken, string RefreshToken);
}

// Fábrica con los límites reales de login (5 por minuto por IP + correo)
public class DentissaFactoryLimitesReales : DentissaFactory
{
    protected override int LimiteLogin => 5;
}

// Fábrica sin clave JWT: la aplicación no debe arrancar
public class DentissaFactorySinClaveJwt : DentissaFactory
{
    protected override string? ClaveJwt => null;
}
