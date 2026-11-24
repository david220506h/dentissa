using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Commit 17 (diseño de la agenda, 9.4): configuración del horario y cierres, para el personal
public class ConfiguracionHorarioTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public ConfiguracionHorarioTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(Local(11, 30, 10));
    }

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    [Fact]
    public async Task Configuracion_LeeRecepcion_EscribeSoloAdmin_ConValidacion_YListaLasCitasFueraDeHorario()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        var sabado = await _f.ConDbAsync(async db =>
        {
            var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, Local(12, 5, 9).UtcDateTime);
            db.Citas.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        });
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);
        var admin = await _f.ClienteConRolAsync(Roles.Admin);

        var actual = await asistente.GetFromJsonAsync<JsonElement>("/api/agenda/configuracion");
        Assert.Equal(60, actual.GetProperty("duracionSlotMinutos").GetInt32());
        Assert.Equal(6, actual.GetProperty("dias").EnumerateArray().Count(d => d.GetProperty("laborable").GetBoolean()));

        // Sábado deja de ser laborable
        var dias = actual.GetProperty("dias").EnumerateArray().Select(d => new
        {
            diaSemana = d.GetProperty("diaSemana").GetInt32(),
            laborable = d.GetProperty("diaSemana").GetInt32() != 6 && d.GetProperty("laborable").GetBoolean(),
            apertura = d.GetProperty("diaSemana").GetInt32() == 6 ? null : d.GetProperty("apertura").GetString(),
            cierre = d.GetProperty("diaSemana").GetInt32() == 6 ? null : d.GetProperty("cierre").GetString(),
        }).ToList();
        object Cuerpo(int liberacion) => new
        {
            duracionSlotMinutos = 60, anticipacionEnLineaAtencionMinutos = 180, anticipacionRecepcionMinutos = 60,
            liberacionAntesCitaAtencionMinutos = liberacion, vidaMaximaSolicitudAtencionMinutos = 720, horizonteReservaDias = 60,
            dias, descansos = JsonSerializer.Deserialize<JsonElement>(actual.GetProperty("descansos").GetRawText())
        };

        Assert.Equal(HttpStatusCode.Forbidden, (await asistente.PutAsJsonAsync("/api/agenda/configuracion", Cuerpo(120))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/agenda/configuracion", Cuerpo(200))).StatusCode); // liberación ≥ anticipación
        var r = await admin.PutAsJsonAsync("/api/agenda/configuracion", Cuerpo(120));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var fuera = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("citasFueraDeHorario").EnumerateArray().Select(c => c.GetProperty("id").GetInt32());
        Assert.Contains(sabado, fuera);
        Assert.Equal(EstadosCita.Pendiente, await _f.ConDbAsync(db => db.Citas.Where(c => c.Id == sabado).Select(c => c.Estado).SingleAsync()));
        Assert.Empty(await _f.CalcularAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 5), servicio, Services.Agenda.CanalReserva.Recepcion));
        Assert.True(await _f.ConDbAsync(db => db.Auditoria.AnyAsync(a => a.Entidad == "horario_dias" && a.Accion == AccionesAuditoria.Modificar)));

        // Se restablece para las demás pruebas de la clase
        await admin.PutAsJsonAsync("/api/agenda/configuracion", new
        {
            duracionSlotMinutos = 60, anticipacionEnLineaAtencionMinutos = 180, anticipacionRecepcionMinutos = 60,
            liberacionAntesCitaAtencionMinutos = 120, vidaMaximaSolicitudAtencionMinutos = 720, horizonteReservaDias = 60,
            dias = actual.GetProperty("dias"), descansos = actual.GetProperty("descansos")
        });
        Assert.NotEmpty(await _f.CalcularAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 5), servicio, Services.Agenda.CanalReserva.Recepcion));
    }

    [Fact]
    public async Task Cierres_CrearDevuelveAfectadas_SoloAdmin_YSeEliminan()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        var afectada = await _f.ConDbAsync(async db =>
        {
            var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, Local(12, 10, 12).UtcDateTime);
            db.Citas.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        });
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);
        var admin = await _f.ClienteConRolAsync(Roles.Admin);
        var cuerpo = new { fecha = "2026-12-10", horaInicio = "12:00:00", horaFin = "16:00:00", motivo = "Capacitación" };

        Assert.Equal(HttpStatusCode.Forbidden, (await asistente.PostAsJsonAsync("/api/agenda/cierres", cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/agenda/cierres", new { fecha = "2026-11-01", motivo = "Pasado" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/agenda/cierres", new { fecha = "2026-12-10", horaInicio = "12:00:00", motivo = "Sin fin" })).StatusCode);
        var r = await admin.PostAsJsonAsync("/api/agenda/cierres", cuerpo);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(afectada, json.GetProperty("citasAfectadas").EnumerateArray().Select(c => c.GetProperty("id").GetInt32()));
        var id = json.GetProperty("cierre").GetProperty("id").GetInt32();
        Assert.Contains(id, (await asistente.GetFromJsonAsync<JsonElement>("/api/agenda/cierres")).EnumerateArray().Select(c => c.GetProperty("id").GetInt32()));
        var horas = (await _f.CalcularAsync(new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 10), servicio, Services.Agenda.CanalReserva.Recepcion)).Select(h => h.Hora.ToString("HH:mm"));
        Assert.Equal(["08:00", "09:00", "10:00"], horas);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/agenda/cierres/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/agenda/cierres/{id}")).StatusCode);
        Assert.True(await _f.ConDbAsync(db => db.Auditoria.AnyAsync(a => a.Entidad == "agenda_cierres" && a.EntidadId == id.ToString() && a.Accion == AccionesAuditoria.Eliminar)));
    }
}
