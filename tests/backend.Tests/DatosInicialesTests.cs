using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend.Tests;

// Cada prueba usa una base propia: el servicio depende de si ya hay administrador
public class DatosInicialesTests
{
    private const string PasswordSemilla = "Semilla-Inicial-2026!";

    [Fact]
    public async Task CreaConfiguracionYAdmin_UnaSolaVez()
    {
        using var factory = new DentissaFactory();
        var correo = $"admin-{Guid.NewGuid():N}@prueba.test";
        var logs = new List<string>();

        await Ejecutar(factory, correo, PasswordSemilla, logs);
        await Ejecutar(factory, correo, PasswordSemilla, logs);

        await factory.ConDbAsync(async db =>
        {
            var config = await db.ConfiguracionAgenda.Include(c => c.Dias).Include(c => c.Descansos).SingleAsync();
            Assert.Equal(60, config.DuracionSlotMinutos);
            Assert.Equal(6, config.Dias.Count(d => d.Laborable));
            Assert.False(config.Dias.Single(d => d.DiaSemana == 7).Laborable);
            Assert.All(config.Dias.Where(d => d.Laborable), d =>
            {
                Assert.Equal(new TimeOnly(8, 0), d.HoraApertura);
                Assert.Equal(new TimeOnly(16, 0), d.HoraCierre);
            });
            Assert.Equal(2, config.Descansos.Count);

            var admins = await db.Usuarios.Where(u => u.Rol == Roles.Admin).ToListAsync();
            var admin = Assert.Single(admins);
            Assert.Equal(correo, admin.Correo);
            Assert.True(BCrypt.Net.BCrypt.Verify(PasswordSemilla, admin.PasswordHash));
            return true;
        });

        // Ni el correo ni la contraseña aparecen en los logs
        Assert.DoesNotContain(logs, l => l.Contains(correo) || l.Contains(PasswordSemilla));
    }

    [Fact]
    public async Task HorariosDelLunes_SonLosSeisAcordados()
    {
        using var factory = new DentissaFactory();
        await Ejecutar(factory, null, null, new List<string>());

        var horarios = await factory.ConDbAsync(async db =>
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IAgendaService>()
                .HorariosDelDiaAsync(new DateOnly(2026, 11, 23)); // lunes
        });

        Assert.Equal(new[] { 8, 9, 10, 12, 13, 15 }, horarios.Select(h => h.Hour));
    }

    [Theory]
    [InlineData(null, PasswordSemilla)]
    [InlineData("admin@prueba.test", null)]
    [InlineData("admin@prueba.test", "corta-11ch")]
    [InlineData("no-es-correo", PasswordSemilla)]
    public async Task DatosFaltantesOInvalidos_NoCreaAdminYAvisaSinValores(string? correo, string? password)
    {
        using var factory = new DentissaFactory();
        var logs = new List<string>();

        await Ejecutar(factory, correo, password, logs);

        Assert.False(await factory.ConDbAsync(db => db.Usuarios.AnyAsync(u => u.Rol == Roles.Admin)));
        Assert.Contains(logs, l => l.Contains("No") && l.Contains("administrador"));
        if (password != null)
            Assert.DoesNotContain(logs, l => l.Contains(password));
        if (correo != null)
            Assert.DoesNotContain(logs, l => l.Contains(correo));
    }

    private static async Task Ejecutar(DentissaFactory factory, string? correo, string? password, List<string> logs)
    {
        // Arranca la API (ya ejecuta el servicio sin datos de semilla)
        _ = factory.Services;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = correo,
                ["Seed:AdminPassword"] = password
            })
            .Build();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
        var servicio = new DatosInicialesService(db, config, new LoggerQueCaptura(logs));
        await servicio.EjecutarAsync();
    }

    private sealed class LoggerQueCaptura(List<string> mensajes) : ILogger<DatosInicialesService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter)
            => mensajes.Add(formatter(state, exception));
    }
}
