using System.Net;
using System.Net.Http.Json;
using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class AuditoriaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public AuditoriaTests(DentissaFactory factory) => _factory = factory;

    // Ningún valor de columna sensible (credenciales, salud, identidad) llega a auditoria
    [Fact]
    public async Task CrearYModificarPaciente_NoGuardaValoresSensibles()
    {
        var marca = Guid.NewGuid().ToString("N")[..10];
        var sensibles = new[]
        {
            ("CURP" + marca.ToUpperInvariant()).PadRight(18, 'X'),
            $"Calle Secreta {marca}",
            $"Penicilina {marca}",
            $"Diabetes {marca}",
            $"Calle Nueva {marca}",
            $"Látex {marca}",
        };

        var pacienteId = await _factory.ConDbAsync(async db =>
        {
            var p = new Paciente
            {
                Nombre = "Auditada", Apellido = marca, Telefono = "5551234567", Origen = OrigenesCita.Recepcion,
                Curp = sensibles[0], Direccion = sensibles[1], Alergias = sensibles[2], CondicionesMedicas = sensibles[3]
            };
            db.Pacientes.Add(p);
            await db.SaveChangesAsync();
            p.Direccion = sensibles[4];
            p.Alergias = sensibles[5];
            p.Telefono = "5559999999";
            await db.SaveChangesAsync();
            return p.Id;
        });

        var filas = await _factory.ConDbAsync(db => db.Auditoria
            .Where(a => a.Entidad == "pacientes" && a.EntidadId == pacienteId.ToString())
            .OrderBy(a => a.Id)
            .ToListAsync());

        Assert.Equal(new[] { AccionesAuditoria.Crear, AccionesAuditoria.Modificar }, filas.Select(f => f.Accion));
        foreach (var fila in filas)
            foreach (var valor in sensibles)
                Assert.DoesNotContain(valor, fila.Detalle ?? "");
        Assert.Contains(AuditoriaInterceptor.Cambio, filas[1].Detalle);
        // Lo no sensible sí queda con antes y después
        Assert.Contains("5559999999", filas[1].Detalle);
    }

    [Fact]
    public async Task CrearUsuario_NoGuardaHashNiToken()
    {
        var usuario = await _factory.CrearUsuarioAsync(Roles.Asistente);

        var detalle = await _factory.ConDbAsync(db => db.Auditoria
            .Where(a => a.Entidad == "usuarios" && a.EntidadId == usuario.Id.ToString() && a.Accion == AccionesAuditoria.Crear)
            .Select(a => a.Detalle)
            .SingleAsync());

        Assert.DoesNotContain(usuario.PasswordHash, detalle);
        Assert.DoesNotContain(usuario.RefreshToken!, detalle);
        Assert.Contains(AuditoriaInterceptor.Cambio, detalle);
    }

    [Fact]
    public async Task Login_RegistraEventosCorrectoYFallido()
    {
        var usuario = await _factory.CrearUsuarioAsync(Roles.Asistente);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new { email = usuario.Correo, password = "incorrecta" });
        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email = usuario.Correo, password = DentissaFactory.PasswordPruebas });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var eventos = await _factory.ConDbAsync(db => db.Auditoria
            .Where(a => a.UsuarioCorreo == usuario.Correo &&
                        (a.Accion == AccionesAuditoria.InicioSesion || a.Accion == AccionesAuditoria.InicioSesionFallido))
            .OrderBy(a => a.Id)
            .ToListAsync());

        Assert.Equal(new[] { AccionesAuditoria.InicioSesionFallido, AccionesAuditoria.InicioSesion }, eventos.Select(e => e.Accion));
        Assert.Null(eventos[0].UsuarioId);
        Assert.Equal(usuario.Id, eventos[1].UsuarioId);
        // El login no genera además una fila "modificar" por el token de renovación
        Assert.False(await _factory.ConDbAsync(db => db.Auditoria.AnyAsync(a =>
            a.Entidad == "usuarios" && a.EntidadId == usuario.Id.ToString() && a.Accion == AccionesAuditoria.Modificar)));
    }

    [Fact]
    public async Task CambioDeCita_GeneraHistorialYBitacora()
    {
        var citaId = await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Asistente);

        var res = await client.PatchAsync($"/api/Citas/{citaId}/confirmar", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var historial = await _factory.ConDbAsync(db => db.CitaHistorial.Where(h => h.CitaId == citaId).ToListAsync());
        Assert.Contains(historial, h => h.Accion == AccionesCitaHistorial.Confirmacion && h.UsuarioId != null);

        var bitacora = await _factory.ConDbAsync(db => db.Auditoria
            .Where(a => a.Entidad == "citas" && a.EntidadId == citaId.ToString() && a.Accion == AccionesAuditoria.Modificar)
            .ToListAsync());
        var fila = Assert.Single(bitacora);
        Assert.Contains("confirmada", fila.Detalle);
        Assert.NotNull(fila.UsuarioId);
    }
}
