using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;

namespace backend.Tests;

// GET /api/citas con filtro por fechas locales, dentista y estado (plan 3a, 14.7)
public class FiltroCitasTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public FiltroCitasTests(FabricaDisponibilidad f) => _f = f;

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private async Task<int> CitaAsync(int dentista, int servicio, int paciente, DateTimeOffset inicio, string estado = EstadosCita.Pendiente) =>
        await _f.ConDbAsync(async db =>
        {
            var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio.UtcDateTime);
            c.Estado = estado;
            db.Citas.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        });

    private static async Task<List<int>> IdsAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Select(c => c.GetProperty("id").GetInt32()).ToList();

    [Fact]
    public async Task RangoInclusivo_EnLaZonaDeLaClinica_YPorDentistaYEstado()
    {
        var (d1, s1, p1) = await _f.DentistaConServicioAsync();
        var (d2, s2, p2) = await _f.DentistaConServicioAsync();
        // 23:30 local del 15-dic ya es 16-dic en UTC: debe contar como 15-dic
        var noche = await CitaAsync(d1, s1, p1, Local(12, 15, 23, 30));
        var temprano = await CitaAsync(d1, s1, p1, Local(12, 14, 0, 0));
        var fuera = await CitaAsync(d1, s1, p1, Local(12, 16, 8, 0));
        var otro = await CitaAsync(d2, s2, p2, Local(12, 15, 9), EstadosCita.Confirmada);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var rango = await IdsAsync(await recepcion.GetAsync("/api/Citas?desde=2026-12-14&hasta=2026-12-15"));
        var porDentista = await IdsAsync(await recepcion.GetAsync($"/api/Citas?desde=2026-12-14&hasta=2026-12-15&dentistaId={d2}"));
        var porEstado = await IdsAsync(await recepcion.GetAsync("/api/Citas?desde=2026-12-14&hasta=2026-12-16&estado=confirmada"));

        Assert.Contains(noche, rango);
        Assert.Contains(temprano, rango);
        Assert.Contains(otro, rango);
        Assert.DoesNotContain(fuera, rango);
        Assert.Equal([otro], porDentista);
        Assert.Equal([otro], porEstado);
    }

    [Theory]
    [InlineData("?desde=2026-12-15&hasta=2026-12-14")]
    [InlineData("?desde=2026-01-01&hasta=2026-12-31")]
    [InlineData("?estado=borrada")]
    public async Task ParametrosInvalidos_400(string consulta)
    {
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        Assert.Equal(HttpStatusCode.BadRequest, (await recepcion.GetAsync("/api/Citas" + consulta)).StatusCode);
    }

    [Fact]
    public async Task SinParametros_ComoAntes_TodasLasCitas()
    {
        var (d, s, p) = await _f.DentistaConServicioAsync();
        var a = await CitaAsync(d, s, p, Local(12, 20, 9));
        var b = await CitaAsync(d, s, p, Local(10, 20, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var ids = await IdsAsync(await recepcion.GetAsync("/api/Citas"));

        Assert.Contains(a, ids);
        Assert.Contains(b, ids);
    }
}
