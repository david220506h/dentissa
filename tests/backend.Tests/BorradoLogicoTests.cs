using System.Net;
using System.Net.Http.Json;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class BorradoLogicoTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public BorradoLogicoTests(DentissaFactory factory) => _factory = factory;

    [Fact]
    public async Task Remove_NoBorraLaFila_SoloLaOculta()
    {
        var usuario = await _factory.CrearUsuarioAsync(Roles.Asistente);

        await _factory.ConDbAsync(async db =>
        {
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == usuario.Id));
            return await db.SaveChangesAsync();
        });

        Assert.False(await _factory.ConDbAsync(db => db.Usuarios.AnyAsync(u => u.Id == usuario.Id)));
        var fila = await _factory.ConDbAsync(db => db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == usuario.Id));
        Assert.NotNull(fila.EliminadoEn);
    }

    [Fact]
    public async Task OcultaPacientesDentistasServiciosYOfertas()
    {
        var citaId = await _factory.CrearCitaAsync();
        var (pacienteId, dentistaId, servicioId) = await EliminarRelacionesAsync(citaId);
        var ofertaId = await _factory.ConDbAsync(async db =>
        {
            var oferta = new Oferta
            {
                ServicioId = servicioId, Descuento = 10,
                FechaInicio = new DateOnly(2026, 1, 1), FechaFin = new DateOnly(2026, 12, 31)
            };
            db.Ofertas.Add(oferta);
            await db.SaveChangesAsync();
            db.Ofertas.Remove(oferta);
            await db.SaveChangesAsync();
            return oferta.Id;
        });

        await _factory.ConDbAsync(async db =>
        {
            Assert.False(await db.Pacientes.AnyAsync(p => p.Id == pacienteId));
            Assert.False(await db.Dentistas.AnyAsync(d => d.Id == dentistaId));
            Assert.False(await db.Servicios.AnyAsync(s => s.Id == servicioId));
            Assert.False(await db.Ofertas.AnyAsync(o => o.Id == ofertaId));
            Assert.True(await db.Pacientes.IgnoreQueryFilters().AnyAsync(p => p.Id == pacienteId));
            Assert.True(await db.Dentistas.IgnoreQueryFilters().AnyAsync(d => d.Id == dentistaId));
            Assert.True(await db.Servicios.IgnoreQueryFilters().AnyAsync(s => s.Id == servicioId));
            Assert.True(await db.Ofertas.IgnoreQueryFilters().AnyAsync(o => o.Id == ofertaId));
            return true;
        });
    }

    // Historial con relaciones eliminadas: la cita sigue en el listado, en GET por id
    // y en mi-agenda con los nombres; las listas para elegir ya no las incluyen.
    [Fact]
    public async Task CitaConRelacionesEliminadas_SigueVisible()
    {
        var (citaId, cuentaDentista) = await _factory.CrearCitaConDentistaAsync();
        var (pacienteId, dentistaId, servicioId) = await EliminarRelacionesAsync(citaId);
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);

        var listado = await admin.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/Citas");
        var enListado = listado!.Single(c => c["id"].ToString() == citaId.ToString());
        Assert.Equal("True", enListado["pacienteEliminado"].ToString(), ignoreCase: true);
        Assert.Equal("True", enListado["dentistaEliminado"].ToString(), ignoreCase: true);
        Assert.Equal("True", enListado["servicioEliminado"].ToString(), ignoreCase: true);
        Assert.Equal("Limpieza", enListado["servicio"].ToString());

        var porId = await admin.GetAsync($"/api/Citas/{citaId}");
        Assert.Equal(HttpStatusCode.OK, porId.StatusCode);

        // Solo la ficha del dentista está eliminada; su cuenta sigue activa
        var agenda = await (await _factory.ClienteDeAsync(cuentaDentista))
            .GetFromJsonAsync<List<Dictionary<string, object>>>("/api/citas/mi-agenda");
        Assert.Contains(agenda!, c => c["id"].ToString() == citaId.ToString());

        var pacientes = await admin.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/Citas/pacientes");
        var dentistas = await admin.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/Citas/dentistas");
        var servicios = await admin.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/Citas/servicios");
        Assert.DoesNotContain(pacientes!, p => p["id"].ToString() == pacienteId.ToString());
        Assert.DoesNotContain(dentistas!, d => d["id"].ToString() == dentistaId.ToString());
        Assert.DoesNotContain(servicios!, s => s["id"].ToString() == servicioId.ToString());
    }

    // Elimina lógicamente paciente y servicio de la cita; el dentista también
    private Task<(int PacienteId, int DentistaId, int ServicioId)> EliminarRelacionesAsync(int citaId)
        => _factory.ConDbAsync(async db =>
        {
            var cita = await db.Citas.IgnoreQueryFilters()
                .Include(c => c.Paciente).Include(c => c.Dentista).Include(c => c.Servicio)
                .SingleAsync(c => c.Id == citaId);
            db.Remove(cita.Paciente);
            db.Remove(cita.Dentista);
            db.Remove(cita.Servicio);
            await db.SaveChangesAsync();
            return (cita.PacienteId, cita.DentistaId, cita.ServicioId);
        });
}

public class CancelacionYDesactivacionTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public CancelacionYDesactivacionTests(DentissaFactory factory) => _factory = factory;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cancelar_SinMotivo_Devuelve400(string? motivo)
    {
        var citaId = await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Asistente);

        var res = await client.PatchAsJsonAsync($"/api/Citas/{citaId}/cancelar", new { motivo });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var estado = await _factory.ConDbAsync(db => db.Citas.Where(c => c.Id == citaId).Select(c => c.Estado).SingleAsync());
        Assert.Equal(EstadosCita.Pendiente, estado);
    }

    [Fact]
    public async Task Cancelar_ConMotivo_GuardaMotivoYNoBorra()
    {
        var citaId = await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Asistente);

        var res = await client.PatchAsJsonAsync($"/api/Citas/{citaId}/cancelar", new { motivo = "El paciente llamó para cancelar" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cita = await _factory.ConDbAsync(db => db.Citas.SingleAsync(c => c.Id == citaId));
        Assert.Equal(EstadosCita.Cancelada, cita.Estado);
        Assert.Equal("El paciente llamó para cancelar", cita.MotivoCancelacion);
        Assert.NotNull(cita.CanceladaEn);

        // Una cita cancelada es final: volver a cancelarla es una transición inválida (diseño 4.2)
        var otra = await client.PatchAsJsonAsync($"/api/Citas/{citaId}/cancelar", new { motivo = "otra vez" });
        Assert.Equal(HttpStatusCode.Conflict, otra.StatusCode);
    }

    [Fact]
    public async Task Editar_ConEstadoCancelada_Devuelve400()
    {
        var citaId = await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Asistente);
        var cita = await client.GetFromJsonAsync<Dictionary<string, object>>($"/api/Citas/{citaId}");

        var res = await client.PutAsJsonAsync($"/api/Citas/{citaId}", new
        {
            dentistaId = int.Parse(cita!["dentistaId"].ToString()!),
            fecha = DateTime.ParseExact(cita["fecha"].ToString()!, "dd/MM/yyyy", null).ToString("yyyy-MM-dd"),
            hora = cita["hora"].ToString() + ":00",
            estado = "cancelada"
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UsuarioDesactivado_NoIniciaSesionNiRenueva()
    {
        var usuario = await _factory.CrearUsuarioAsync(Roles.Asistente);
        var anonimo = _factory.CreateClient();
        var login = await anonimo.PostAsJsonAsync("/api/auth/login", new { email = usuario.Correo, password = DentissaFactory.PasswordPruebas });
        var sesion = await login.Content.ReadFromJsonAsync<DentissaFactory.LoginRespuesta>();

        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var res = await admin.PatchAsync($"/api/Usuarios/{usuario.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var otroLogin = await anonimo.PostAsJsonAsync("/api/auth/login", new { email = usuario.Correo, password = DentissaFactory.PasswordPruebas });
        Assert.Equal(HttpStatusCode.Unauthorized, otroLogin.StatusCode);
        Assert.Contains("Credenciales incorrectas", await otroLogin.Content.ReadAsStringAsync());

        var refresh = await anonimo.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = sesion!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var listado = await admin.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/Usuarios");
        var fila = listado!.Single(u => u["id"].ToString() == usuario.Id.ToString());
        Assert.Equal("False", fila["activo"].ToString(), ignoreCase: true);

        var activar = await admin.PatchAsync($"/api/Usuarios/{usuario.Id}/activar", null);
        Assert.Equal(HttpStatusCode.OK, activar.StatusCode);
        var deNuevo = await anonimo.PostAsJsonAsync("/api/auth/login", new { email = usuario.Correo, password = DentissaFactory.PasswordPruebas });
        Assert.Equal(HttpStatusCode.OK, deNuevo.StatusCode);
    }

    [Fact]
    public async Task UsuarioEliminado_NoIniciaSesion()
    {
        var usuario = await _factory.CrearUsuarioAsync(Roles.Asistente);
        await _factory.ConDbAsync(async db =>
        {
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == usuario.Id));
            return await db.SaveChangesAsync();
        });

        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = usuario.Correo, password = DentissaFactory.PasswordPruebas });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Admin_NoPuedeDesactivarseASiMismo()
    {
        var cuenta = await _factory.CrearUsuarioAsync(Roles.Admin);
        var admin = await _factory.ClienteDeAsync(cuenta);

        var res = await admin.PatchAsync($"/api/Usuarios/{cuenta.Id}/desactivar", null);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task DeleteFisico_YaNoExiste()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var citaId = await _factory.CrearCitaAsync();

        var cita = await admin.DeleteAsync($"/api/Citas/{citaId}");
        var usuario = await admin.DeleteAsync("/api/Usuarios/1");

        Assert.Contains(cita.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(usuario.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.True(await _factory.ConDbAsync(db => db.Citas.AnyAsync(c => c.Id == citaId)));
    }
}
