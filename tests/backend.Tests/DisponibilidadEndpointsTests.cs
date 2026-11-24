using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Fase 2, commit 5 (diseño de la agenda, 9.1 y 9.4): endpoints de disponibilidad, con reloj fijo.
// "Ahora": sábado 28-nov-2026 20:00 en la clínica; el lunes es 30-nov.
public class DisponibilidadEndpointsTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public DisponibilidadEndpointsTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Publico_SinCredencial_SoloHorasConReglasEnLinea_YTodosLosDias()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        var r = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?servicioId={servicio}&desde=2026-11-29&hasta=2026-11-30");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dias = await JsonAsync(r);
        Assert.Equal(2, dias.GetArrayLength());
        Assert.Equal("2026-11-29", dias[0].GetProperty("fecha").GetString());
        Assert.Equal(0, dias[0].GetProperty("horas").GetArrayLength()); // domingo
        Assert.Equal(["12:00", "13:00", "15:00"], dias[1].GetProperty("horas").EnumerateArray().Select(h => h.GetString()));
        // Nunca expone dentistas
        Assert.DoesNotContain("dentista", (await r.Content.ReadAsStringAsync()).ToLowerInvariant());
    }

    [Theory]
    [InlineData("servicioId=0&desde=2026-11-30&hasta=2026-11-30", "PARAMETROS_INVALIDOS")]
    [InlineData("servicioId=1&desde=30-11-2026&hasta=2026-11-30", "PARAMETROS_INVALIDOS")]
    [InlineData("servicioId=999999&desde=2026-11-30&hasta=2026-11-30", "PARAMETROS_INVALIDOS")]
    public async Task Publico_ParametrosOServicioInvalidos_400Generico(string consulta, string codigo)
    {
        var r = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(codigo, (await JsonAsync(r)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Publico_ServicioInactivo_MismoMensajeQueInexistente()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db => db.Servicios.Where(s => s.Id == servicio).ExecuteUpdateAsync(s => s.SetProperty(x => x.Activo, false)));

        var inactivo = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?servicioId={servicio}&desde=2026-11-30&hasta=2026-11-30");
        var inexistente = await _f.CreateClient().GetAsync("/api/publico/disponibilidad?servicioId=999999&desde=2026-11-30&hasta=2026-11-30");

        Assert.Equal(HttpStatusCode.BadRequest, inactivo.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await inactivo.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Publico_MaximoCatorceDias()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();

        var ok = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?servicioId={servicio}&desde=2026-11-30&hasta=2026-12-13");
        var largo = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?servicioId={servicio}&desde=2026-11-30&hasta=2026-12-14");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, largo.StatusCode);
        Assert.Equal("RANGO_INVALIDO", (await JsonAsync(largo)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Recepcion_ConDentistasLibres_YReglasDeRecepcion()
    {
        var (dentista, servicio, _) = await _f.DentistaConServicioAsync();
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await asistente.GetAsync($"/api/agenda/disponibilidad?fecha=2026-11-30&servicioId={servicio}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var horarios = await JsonAsync(r);
        // Recepción: anticipación en minutos de reloj, así que 8, 9 y 10 del lunes sí se ofrecen
        Assert.Equal(["08:00", "09:00", "10:00", "12:00", "13:00", "15:00"], horarios.EnumerateArray().Select(h => h.GetProperty("hora").GetString()));
        var libre = horarios[0].GetProperty("dentistasLibres")[0];
        Assert.Equal(dentista, libre.GetProperty("id").GetInt32());
        Assert.Equal("Dentista Prueba", libre.GetProperty("nombre").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(Roles.Dentista, HttpStatusCode.Forbidden)]
    [InlineData(Roles.Paciente, HttpStatusCode.Forbidden)]
    public async Task Recepcion_SoloAsistenteYAdministracion(string? rol, HttpStatusCode esperado)
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        var client = rol is null ? _f.CreateClient() : await _f.ClienteConRolAsync(rol);

        var r = await client.GetAsync($"/api/agenda/disponibilidad?fecha=2026-11-30&servicioId={servicio}");

        Assert.Equal(esperado, r.StatusCode);
    }

    [Fact]
    public async Task HorariosDisponibles_Compatibilidad_DelegaEnElMotor()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db =>
        {
            db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentista, servicio, FabricaDisponibilidad.Utc(30, 9)));
            return db.SaveChangesAsync();
        });
        var admin = await _f.ClienteConRolAsync(Roles.Admin);

        var r = await admin.GetAsync($"/api/Citas/horarios-disponibles?fecha=2026-11-30&dentistaId={dentista}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(["08:00", "10:00", "12:00", "13:00", "15:00"], await r.Content.ReadFromJsonAsync<string[]>());
    }
}

public class DisponibilidadLimitesTests : IClassFixture<DentissaFactoryLimitesReales>
{
    private readonly DentissaFactoryLimitesReales _f;

    public DisponibilidadLimitesTests(DentissaFactoryLimitesReales f) => _f = f;

    [Fact]
    public async Task Publico_LimiteDeTasaPorIp_429()
    {
        var client = _f.CreateClient();
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 6; i++)
            ultima = await client.GetAsync("/api/publico/disponibilidad?servicioId=0&desde=2026-11-30&hasta=2026-11-30");

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
    }
}

public class DisponibilidadSinConfiguracionTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public DisponibilidadSinConfiguracionTests(FabricaDisponibilidad f) => _f = f;

    [Fact]
    public async Task Publico_AgendaSinConfigurar_503()
    {
        var (_, servicio, _) = await _f.DentistaConServicioAsync();
        await _f.ConDbAsync(db => db.Database.ExecuteSqlRawAsync("TRUNCATE configuracion_agenda CASCADE"));

        var r = await _f.CreateClient().GetAsync($"/api/publico/disponibilidad?servicioId={servicio}&desde=2026-11-30&hasta=2026-11-30");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("AGENDA_NO_CONFIGURADA", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }
}
