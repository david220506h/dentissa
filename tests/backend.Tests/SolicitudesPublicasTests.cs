using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Bloque C: POST /api/publico/solicitudes y la bandeja GET /api/solicitudes. Reloj fijo: sábado 28-nov-2026
// 20:00 en la clínica; el lunes 30-nov a las 12:00 es el primer horario que acepta el canal en línea.
public class SolicitudesPublicasTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public SolicitudesPublicasTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);
    }

    internal static object Cuerpo(int servicio, string correo, string hora = "12:00", bool aviso = true, string telefono = "55 1234 5678",
        string nombre = "Ana", string? trampa = null) => new
    {
        servicioId = servicio, fecha = "2026-11-30", hora, nombre, apellido = "Prueba", telefono, correo,
        motivo = "Revisión", aceptaAviso = aviso, sitioWeb = trampa,
    };

    private static string Correo() => $"visita-{Guid.NewGuid():N}@prueba.test";

    [Fact]
    public async Task Valida_ApartaElHorario_YApareceEnLaBandeja()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();

        var r = await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cuerpo = await r.Content.ReadAsStringAsync();
        Assert.DoesNotContain("dentista", cuerpo.ToLowerInvariant());
        var cita = await _f.ConDbAsync(db => db.Citas.Include(c => c.Paciente).SingleAsync(c => c.Paciente.Correo == correo));
        Assert.Equal(EstadosCita.Pendiente, cita.Estado);
        Assert.Equal(OrigenesCita.Web, cita.Origen);
        Assert.NotNull(cita.BloqueoExpiraEn);
        Assert.Equal(OrigenesCita.Web, cita.Paciente.Origen);

        // Ocupa el horario: la segunda solicitud al mismo horario (único dentista del servicio) choca
        var otra = await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, Correo()));
        Assert.Equal(HttpStatusCode.Conflict, otra.StatusCode);

        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        var bandeja = await recepcion.GetFromJsonAsync<JsonElement>("/api/solicitudes");
        Assert.Contains(bandeja.EnumerateArray(), s => s.GetProperty("id").GetInt32() == cita.Id && s.GetProperty("correo").GetString() == correo);
    }

    [Fact]
    public async Task Confirmar_SaleDeLaBandeja_YQuedaConfirmada()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();
        await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo, "13:00"));
        var id = await _f.ConDbAsync(db => db.Citas.Where(c => c.Paciente.Correo == correo).Select(c => c.Id).SingleAsync());
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsJsonAsync($"/api/citas/{id}/confirmar", new { canal = "panel" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var bandeja = await recepcion.GetFromJsonAsync<JsonElement>("/api/solicitudes");
        Assert.DoesNotContain(bandeja.EnumerateArray(), s => s.GetProperty("id").GetInt32() == id);
        var cita = await _f.ConDbAsync(db => db.Citas.SingleAsync(c => c.Id == id));
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Null(cita.BloqueoExpiraEn);
    }

    [Fact]
    public async Task Rechazar_ConMotivo_LiberaElHorario()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();
        await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo, "15:00"));
        var id = await _f.ConDbAsync(db => db.Citas.Where(c => c.Paciente.Correo == correo).Select(c => c.Id).SingleAsync());
        var recepcion = await _f.ClienteConRolAsync(Roles.Admin);

        var r = await recepcion.PatchAsJsonAsync($"/api/citas/{id}/cancelar", new { motivo = "La clínica no tiene ese horario disponible" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var nueva = await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, Correo(), "15:00"));
        Assert.Equal(HttpStatusCode.Created, nueva.StatusCode);
    }

    [Fact]
    public async Task MismoCorreo_ReutilizaLaFicha()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var (_, otro, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();

        await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo, "12:00"));
        await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(otro, correo.ToUpperInvariant(), "13:00"));

        Assert.Equal(1, await _f.ConDbAsync(db => db.Pacientes.CountAsync(p => p.Correo == correo)));
        Assert.Equal(2, await _f.ConDbAsync(db => db.Citas.CountAsync(c => c.Paciente.Correo == correo)));
    }

    [Fact]
    public async Task CampoTrampa_RespondeIgual_SinCrearNada()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();

        var r = await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo, trampa: "https://spam.example"));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.False(await _f.ConDbAsync(db => db.Pacientes.AnyAsync(p => p.Correo == correo)));
    }

    [Theory]
    [InlineData(false, "55 1234 5678", "Ana", "12:00")]   // sin aviso de privacidad
    [InlineData(true, "1234", "Ana", "12:00")]            // teléfono corto
    [InlineData(true, "55-ABC-12345678", "Ana", "12:00")] // teléfono con letras
    [InlineData(true, "55 1234 5678", "Ana123", "12:00")] // nombre con números
    [InlineData(true, "55 1234 5678", "Ana", "11:00")]    // descanso: no es horario
    [InlineData(true, "55 1234 5678", "Ana", "09:00")]    // sin la anticipación en línea
    public async Task Invalida_400_SinFichaHuerfana(bool aviso, string telefono, string nombre, string hora)
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var correo = Correo();

        var r = await _f.CreateClient().PostAsJsonAsync("/api/publico/solicitudes", Cuerpo(servicio, correo, hora, aviso, telefono, nombre));

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.False(await _f.ConDbAsync(db => db.Pacientes.AnyAsync(p => p.Correo == correo)));
    }

    [Fact]
    public async Task Bandeja_SoloRecepcion()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/solicitudes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _f.ClienteConRolAsync(Roles.Dentista)).GetAsync("/api/solicitudes")).StatusCode);
    }

    [Fact]
    public async Task ServiciosPublicos_SinSesion()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var lista = await _f.CreateClient().GetFromJsonAsync<JsonElement>("/api/publico/servicios");
        Assert.Contains(lista.EnumerateArray(), s => s.GetProperty("id").GetInt32() == servicio);
    }
}

public class FabricaLimiteSolicitudes : FabricaDisponibilidad
{
    protected override int LimiteLogin => 3;
}

public class LimiteSolicitudesTests(FabricaLimiteSolicitudes f) : IClassFixture<FabricaLimiteSolicitudes>
{
    [Fact]
    public async Task MasDeLasPermitidasPorIp_429()
    {
        var (_, servicio, _) = await f.DentistaConServicioAsync();
        var cliente = f.CreateClient();
        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            codigos.Add((await cliente.PostAsJsonAsync("/api/publico/solicitudes",
                SolicitudesPublicasTests.Cuerpo(servicio, $"limite-{i}-{Guid.NewGuid():N}@prueba.test", trampa: "robot"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, codigos[^1]);
    }
}
