using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Commit 18: bandeja interna del personal
public class BandejaInternaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _f;

    public BandejaInternaTests(DentissaFactory f) => _f = f;

    private Task<long> AvisoAsync(int usuario, string asunto, bool leida = false) => _f.ConDbAsync(async db =>
    {
        var n = new Notificacion { UsuarioId = usuario, Tipo = TiposNotificacion.EnvioFallido, Canal = CanalesNotificacion.Interna, Asunto = asunto, Mensaje = "Prueba", Leida = leida, CreadoEn = DateTime.UtcNow };
        db.Notificaciones.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    });

    [Fact]
    public async Task CadaCuentaVeSoloSusAvisos_YLosMarcaLeidos()
    {
        var yo = await _f.CrearUsuarioAsync(Roles.Asistente);
        var otra = await _f.CrearUsuarioAsync(Roles.Asistente);
        var mio = await AvisoAsync(yo.Id, "Mío");
        await AvisoAsync(yo.Id, "Viejo", leida: true);
        var ajeno = await AvisoAsync(otra.Id, "Ajeno");
        var client = await _f.ClienteDeAsync(yo);

        var todos = await client.GetFromJsonAsync<JsonElement>("/api/notificaciones/internas");
        var noLeidos = await client.GetFromJsonAsync<JsonElement>("/api/notificaciones/internas?noLeidas=true");

        Assert.Equal(2, todos.GetProperty("notificaciones").GetArrayLength());
        Assert.Equal(1, todos.GetProperty("sinLeer").GetInt32());
        Assert.Equal([mio], noLeidos.GetProperty("notificaciones").EnumerateArray().Select(n => n.GetProperty("id").GetInt64()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync($"/api/notificaciones/{ajeno}/leida", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/api/notificaciones/{mio}/leida", null)).StatusCode);
        Assert.True(await _f.ConDbAsync(db => db.Notificaciones.Where(n => n.Id == mio).Select(n => n.Leida).SingleAsync()));
        Assert.False(await _f.ConDbAsync(db => db.Notificaciones.Where(n => n.Id == ajeno).Select(n => n.Leida).SingleAsync()));
    }

    [Theory]
    [InlineData(Roles.Dentista, HttpStatusCode.Forbidden)]
    [InlineData(Roles.Paciente, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task SoloRecepcionYAdministracion(string? rol, HttpStatusCode esperado)
    {
        var client = rol is null ? _f.CreateClient() : await _f.ClienteConRolAsync(rol);

        Assert.Equal(esperado, (await client.GetAsync("/api/notificaciones/internas")).StatusCode);
    }
}
