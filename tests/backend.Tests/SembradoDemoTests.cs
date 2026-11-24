using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

// Sembrado de la demo (docs/DEMO.md): datos ficticios solo sobre una base vacía y con Demo:Habilitada
public class SembradoDemoTests
{
    // Martes 24 nov 2026, 11:30 en la clínica
    private static readonly DateTimeOffset Ahora = new(2026, 11, 24, 17, 30, 0, TimeSpan.Zero);

    private sealed class FabricaDemo(bool habilitada) : DentissaFactory
    {
        public override TimeProvider? Tiempo { get; } = new RelojFijo(Ahora);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Demo:Habilitada", habilitada.ToString());
        }
    }

    private static async Task<IReadOnlyList<SembradoDemoService.Cuenta>> Sembrar(DentissaFactory f)
    {
        using var scope = f.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SembradoDemoService>().EjecutarAsync();
    }

    [Fact]
    public async Task SiembraPersonalPacientesYCitasEnTodosLosEstados()
    {
        using var f = new FabricaDemo(habilitada: true);
        var cuentas = await Sembrar(f);

        Assert.Equal(new[] { Roles.Admin, Roles.Asistente, Roles.Dentista, Roles.Dentista, Roles.Dentista }, cuentas.Select(c => c.Rol));
        Assert.All(cuentas, c =>
        {
            Assert.EndsWith(".test", c.Correo);
            Assert.Equal(16, c.Password.Length);
        });
        Assert.Equal(cuentas.Count, cuentas.Select(c => c.Password).Distinct().Count());

        await f.ConDbAsync(async db =>
        {
            var admin = await db.Usuarios.SingleAsync(u => u.Rol == Roles.Admin);
            Assert.True(BCrypt.Net.BCrypt.Verify(cuentas[0].Password, admin.PasswordHash));
            Assert.Equal(3, await db.Dentistas.CountAsync());

            var pacientes = await db.Pacientes.ToListAsync();
            Assert.Equal(30, pacientes.Count);
            Assert.All(pacientes.Where(p => p.Correo != null), p => Assert.EndsWith("@correo.test", p.Correo));
            Assert.InRange(pacientes.Count(p => p.Alergias != null), 5, 15);
            Assert.InRange(pacientes.Count(p => p.UsuarioId != null), 5, 10);

            var citas = await db.Citas.ToListAsync();
            Assert.All(EstadosCita.Todos, e => Assert.Contains(citas, c => c.Estado == e));
            // Ninguna activa termina en el pasado; las completadas y no asistidas ya terminaron
            var ahora = Ahora.UtcDateTime;
            Assert.DoesNotContain(citas, c => c.Estado is EstadosCita.Pendiente && c.Inicio <= ahora);
            Assert.All(citas.Where(c => c.Estado is EstadosCita.Completada or EstadosCita.NoAsistio), c => Assert.True(c.Fin <= ahora));
            Assert.All(citas.Where(c => c.Estado == EstadosCita.Cancelada), c => Assert.NotNull(c.MotivoCancelacion));
            // Cada cita tiene su historial de creación
            Assert.Equal(citas.Count, await db.CitaHistorial.CountAsync(h => h.Accion == AccionesCitaHistorial.Creacion));
            return true;
        });
    }

    [Fact]
    public async Task NoSiembraSobreUnaBaseConDatos()
    {
        using var f = new FabricaDemo(habilitada: true);
        await Sembrar(f);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Sembrar(f));
        Assert.Contains("base vacía", error.Message);
        Assert.Equal(30, await f.ConDbAsync(db => db.Pacientes.CountAsync()));
    }

    [Fact]
    public async Task SinDemoHabilitada_NoSiembra()
    {
        using var f = new FabricaDemo(habilitada: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sembrar(f));
        Assert.False(await f.ConDbAsync(db => db.Usuarios.AnyAsync()));
    }
}
