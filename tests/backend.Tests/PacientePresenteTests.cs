using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Cita con el paciente presente (decisión del dueño, 2026-11-24): solo recepción; sin anticipación de 60 minutos;
// un inicio pasado solo dentro de 15 minutos de atención; el horizonte sigue.
public class PacientePresenteTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public PacientePresenteTests(FabricaDisponibilidad f) => _f = f;

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0, int anio = 2026) =>
        new(anio, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private async Task<(HttpResponseMessage Respuesta, int Dentista)> PedirAsync(
        DateTimeOffset ahora, string fecha, string hora, bool? presente, string rol = Roles.Asistente, bool anonimo = false)
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        _f.Reloj.Fijar(ahora);
        var client = anonimo ? _f.CreateClient() : await _f.ClienteConRolAsync(rol);
        object cuerpo = presente is null
            ? new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha, hora }
            : new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha, hora, pacientePresente = presente };
        return (await client.PostAsJsonAsync("/api/Citas", cuerpo), dentista);
    }

    private static async Task<string?> CodigoAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString();

    [Theory]
    [InlineData(Roles.Asistente)]
    [InlineData(Roles.Admin)]
    public async Task ConIndicador_SinAnticipacion_YQuedaEnCitaHistorialYBitacora(string rol)
    {
        // 30 minutos antes: sin el indicador sería ANTICIPACION_INSUFICIENTE
        var (r, dentista) = await PedirAsync(Local(12, 1, 14, 30), "2026-12-01", "15:00:00", true, rol);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cita = await _f.ConDbAsync(db => db.Citas.AsNoTracking().Include(c => c.Historial).SingleAsync(c => c.DentistaId == dentista));
        Assert.True(cita.PacientePresente);
        Assert.Null(cita.BloqueoExpiraEn);
        var alta = Assert.Single(cita.Historial);
        Assert.True(JsonDocument.Parse(alta.Datos!).RootElement.GetProperty("pacientePresente").GetBoolean());
        var bitacora = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .SingleAsync(a => a.Entidad == "citas" && a.EntidadId == cita.Id.ToString() && a.Accion == AccionesAuditoria.Crear));
        Assert.True(JsonDocument.Parse(bitacora.Detalle!).RootElement.GetProperty("valores").GetProperty("paciente_presente").GetBoolean());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public async Task SinIndicador_TodoSigueIgual(bool? presente)
    {
        var (r, dentista) = await PedirAsync(Local(12, 1, 14, 30), "2026-12-01", "15:00:00", presente);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("ANTICIPACION_INSUFICIENTE", await CodigoAsync(r));
        Assert.False(await _f.ConDbAsync(db => db.Citas.AnyAsync(c => c.DentistaId == dentista)));
    }

    [Theory]
    [InlineData(13, 15, "13:00:00", HttpStatusCode.Created, null)]           // 15 min después del inicio: dentro de la tolerancia
    [InlineData(13, 16, "13:00:00", HttpStatusCode.BadRequest, "HORARIO_PASADO")]
    [InlineData(14, 30, "13:00:00", HttpStatusCode.BadRequest, "HORARIO_PASADO")] // en el descanso: 60 min de atención
    [InlineData(12, 5, "12:00:00", HttpStatusCode.Created, null)]
    public async Task InicioPasado_SoloDentroDeQuinceMinutosDeAtencion(int hora, int minuto, string cita, HttpStatusCode esperado, string? codigo)
    {
        var (r, _) = await PedirAsync(Local(12, 1, hora, minuto), "2026-12-01", cita, true);

        Assert.Equal(esperado, r.StatusCode);
        if (codigo != null) Assert.Equal(codigo, await CodigoAsync(r));
    }

    [Fact]
    public async Task ConIndicador_ElHorizonteSigue()
    {
        var (r, _) = await PedirAsync(Local(11, 30, 10), "2027-02-02", "08:00:00", true);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("FUERA_DE_HORIZONTE", await CodigoAsync(r));
    }

    [Theory]
    [InlineData(Roles.Paciente, HttpStatusCode.Forbidden)]
    [InlineData(Roles.Dentista, HttpStatusCode.Forbidden)]
    public async Task OtrosRoles_NoPuedenEnviarlo(string rol, HttpStatusCode esperado)
    {
        var (r, dentista) = await PedirAsync(Local(12, 1, 14, 30), "2026-12-01", "15:00:00", true, rol);

        Assert.Equal(esperado, r.StatusCode);
        Assert.False(await _f.ConDbAsync(db => db.Citas.AnyAsync(c => c.DentistaId == dentista)));
    }

    [Fact]
    public async Task ElPublico_NoPuedeEnviarlo()
    {
        var (r, dentista) = await PedirAsync(Local(12, 1, 14, 30), "2026-12-01", "15:00:00", true, anonimo: true);

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.False(await _f.ConDbAsync(db => db.Citas.AnyAsync(c => c.DentistaId == dentista)));
    }
}

public class ReglaPacientePresenteTests
{
    // Función pura con la configuración sembrada; fechas en hora local ("yyyy-MM-dd HH:mm")
    [Theory]
    [InlineData("2026-12-01 10:14", "2026-12-01 10:00", true)]
    [InlineData("2026-12-01 10:15", "2026-12-01 10:00", true)]   // límite exacto: 15 min de atención
    [InlineData("2026-12-01 10:16", "2026-12-01 10:00", false)]
    [InlineData("2026-12-01 12:10", "2026-12-01 10:00", false)]  // 70 min de atención (descanso en medio)
    [InlineData("2026-12-01 14:50", "2026-12-01 13:00", false)]  // en el descanso: 60 min de atención
    [InlineData("2026-12-01 08:59", "2026-12-01 09:00", true)]   // un minuto antes: sin anticipación mínima
    [InlineData("2026-11-30 10:00", "2027-02-02 08:00", false)]  // horizonte
    public void Regla_Pura(string ahora, string inicio, bool aceptada)
    {
        var r = ReglaBloqueo.CalcularBloqueo(
            RelojAtencionTests.L(ahora), RelojAtencionTests.L(inicio), CanalReserva.PacientePresente,
            new RelojAtencion(RelojAtencionTests.Horario()), RelojAtencionTests.Parametros);

        Assert.Equal(aceptada, r.Aceptada);
        Assert.Null(r.BloqueoExpiraEn);
    }
}
