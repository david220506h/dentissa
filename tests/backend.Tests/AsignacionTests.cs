using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Repositories;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Fase 2, commit 8 (diseño de la agenda, sección 3): asignación dentro de la reserva y reasignación.
public static class DatosAsignacion
{
    // Servicio ligado a n dentistas activos (ids ascendentes) y un paciente
    public static Task<(int Servicio, int[] Dentistas, int Paciente)> ServicioConDentistasAsync(DentissaFactory f, int n) => f.ConDbAsync(async db =>
    {
        var servicio = new Servicio
        {
            Nombre = "Servicio", Precio = 600, DuracionSlots = 1,
            Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") }
        };
        var dentistas = Enumerable.Range(0, n).Select(i => new Dentista
        {
            Usuario = new Usuario
            {
                Nombre = "Dentista", Apellido = "Prueba " + i, Correo = $"dent-{Guid.NewGuid():N}@prueba.test",
                PasswordHash = "x", Rol = Roles.Dentista
            },
            Especialidad = "General"
        }).ToList();
        var paciente = new Paciente { Nombre = "Paciente", Apellido = "Prueba", Telefono = "5550001111", Origen = OrigenesCita.Recepcion };
        db.Add(servicio);
        db.AddRange(dentistas);
        db.Add(paciente);
        await db.SaveChangesAsync();
        foreach (var d in dentistas)
            db.DentistaServicios.Add(new DentistaServicio { DentistaId = d.Id, ServicioId = servicio.Id });
        await db.SaveChangesAsync();
        return (servicio.Id, dentistas.Select(d => d.Id).Order().ToArray(), paciente.Id);
    });
}

public class AsignacionTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public AsignacionTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(Local(11, 30, 10, 30)); // lunes 30-nov, 10:30
    }

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private Task CitaAsync(int paciente, int dentista, int servicio, DateTimeOffset inicio, string estado = EstadosCita.Confirmada) => _f.ConDbAsync(db =>
    {
        var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio.UtcDateTime);
        c.Estado = estado;
        db.Citas.Add(c);
        return db.SaveChangesAsync();
    });

    private Task<int> OtroPacienteAsync() => _f.ConDbAsync(async db =>
    {
        var p = new Paciente { Nombre = "Otra", Apellido = "Persona", Telefono = "5550002222", Origen = OrigenesCita.Recepcion };
        db.Pacientes.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    });

    private async Task<(HttpResponseMessage Respuesta, int? Dentista)> AgendarAsync(int paciente, int servicio, string fecha = "2026-12-01", string hora = "09:00:00", int? dentistaId = null)
    {
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, servicioId = servicio, fecha, hora, dentistaId });
        int? asignado = r.StatusCode == HttpStatusCode.Created ? (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("dentistaId").GetInt32() : null;
        return (r, asignado);
    }

    [Fact]
    public async Task UnSoloDentista_SeAsigna_YSiEstaOcupado409()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 1);

        var (r1, asignado) = await AgendarAsync(paciente, servicio);
        var (r2, _) = await AgendarAsync(await OtroPacienteAsync(), servicio);

        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(d[0], asignado);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
        Assert.Equal(HorarioOcupadoException.MensajeSinDentistas, (await r2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Continuidad_GanaAunqueTengaMasCarga()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        await CitaAsync(paciente, d[1], servicio, Local(11, 23, 9), EstadosCita.Completada); // ya lo atendió
        var otro = await OtroPacienteAsync();
        await CitaAsync(otro, d[1], servicio, Local(12, 1, 12));                         // más carga ese día

        var (_, asignado) = await AgendarAsync(paciente, servicio);

        Assert.Equal(d[1], asignado);
    }

    [Fact]
    public async Task SinContinuidad_MenorCargaDelDia()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var otro = await OtroPacienteAsync();
        await CitaAsync(otro, d[0], servicio, Local(12, 1, 12));
        await CitaAsync(otro, d[0], servicio, Local(12, 1, 13));
        await CitaAsync(otro, d[1], servicio, Local(12, 2, 9)); // carga de semana de d[1], no del día

        var (_, asignado) = await AgendarAsync(paciente, servicio);

        Assert.Equal(d[1], asignado);
    }

    [Fact]
    public async Task DiaEmpatado_MenorCargaDeLaSemana()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var otro = await OtroPacienteAsync();
        await CitaAsync(otro, d[0], servicio, Local(12, 3, 9));
        await CitaAsync(otro, d[0], servicio, Local(12, 4, 9));
        await CitaAsync(otro, d[1], servicio, Local(12, 5, 9));
        await CitaAsync(otro, d[1], servicio, Local(12, 7, 9)); // lunes siguiente: otra semana, no cuenta

        var (_, asignado) = await AgendarAsync(paciente, servicio);

        Assert.Equal(d[1], asignado);
    }

    [Fact]
    public async Task TodoEmpatado_IdMenor_YCanceladasNoCuentan()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 3);
        var otro = await OtroPacienteAsync();
        await _f.ConDbAsync(db =>
        {
            var c = DentissaFactory.NuevaCita(otro, d[1], servicio, Local(12, 1, 12).UtcDateTime);
            c.Estado = EstadosCita.Cancelada;
            c.MotivoCancelacion = "Prueba";
            c.CanceladaEn = Local(11, 30, 9).UtcDateTime;
            db.Citas.Add(c);
            return db.SaveChangesAsync();
        });
        await CitaAsync(otro, d[0], servicio, Local(12, 1, 13));

        var (_, asignado) = await AgendarAsync(paciente, servicio);

        Assert.Equal(d[1], asignado); // d[0] tiene una activa; d[1] y d[2] empatan y gana el id menor
    }

    [Fact]
    public async Task DentistaIndicado_NoLigadoAlServicio_400()
    {
        var (servicio, _, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 1);
        var (_, ajeno, _) = await DatosAsignacion.ServicioConDentistasAsync(_f, 1);

        var (r, _) = await AgendarAsync(paciente, servicio, dentistaId: ajeno[0]);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ── Reasignación ─────────────────────────────────────
    private async Task<int> CitaPendienteAsync(int paciente, int dentista, int servicio, DateTimeOffset inicio) => await _f.ConDbAsync(async db =>
    {
        var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio.UtcDateTime);
        db.Citas.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    });

    [Fact]
    public async Task Reasignar_MismoHorario_HistorialYBitacora_SinCorreo()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var cita = await CitaPendienteAsync(paciente, d[0], servicio, Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        var notificacionesAntes = await _f.ConDbAsync(db => db.Notificaciones.CountAsync());

        var r = await recepcion.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = d[1], motivo = "Agenda del dentista" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var guardada = await _f.ConDbAsync(db => db.Citas.AsNoTracking().Include(c => c.Historial).SingleAsync(c => c.Id == cita));
        Assert.Equal(d[1], guardada.DentistaId);
        Assert.Equal(Local(12, 1, 9).UtcDateTime, guardada.Inicio);
        Assert.Equal("Agenda del dentista", guardada.MotivoCambio);
        var h = Assert.Single(guardada.Historial, x => x.Accion == AccionesCitaHistorial.Reasignacion);
        Assert.Contains(d[0].ToString(), h.Datos);
        var fila = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .SingleAsync(a => a.Entidad == "citas" && a.EntidadId == cita.ToString() && a.Accion == AccionesAuditoria.Modificar));
        var dentistaId = JsonDocument.Parse(fila.Detalle!).RootElement.GetProperty("cambios").GetProperty("dentista_id");
        Assert.Equal(d[0], dentistaId.GetProperty("antes").GetInt32());
        Assert.Equal(d[1], dentistaId.GetProperty("despues").GetInt32());
        Assert.Equal(notificacionesAntes, await _f.ConDbAsync(db => db.Notificaciones.CountAsync()));
    }

    [Fact]
    public async Task Reasignar_Ocupado409_NoElegible400_MismoDentista409_SinMotivo400()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var (_, ajeno, _) = await DatosAsignacion.ServicioConDentistasAsync(_f, 1);
        var cita = await CitaPendienteAsync(paciente, d[0], servicio, Local(12, 1, 10));
        await CitaAsync(await OtroPacienteAsync(), d[1], servicio, Local(12, 1, 10));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var ocupado = await recepcion.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = d[1], motivo = "x" });
        var noElegible = await recepcion.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = ajeno[0], motivo = "x" });
        var mismo = await recepcion.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = d[0], motivo = "x" });
        var sinMotivo = await recepcion.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = d[1] });

        Assert.Equal(HttpStatusCode.Conflict, ocupado.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noElegible.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, mismo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
        Assert.Equal(d[0], await _f.ConDbAsync(db => db.Citas.Where(c => c.Id == cita).Select(c => c.DentistaId).SingleAsync()));
    }

    [Fact]
    public async Task Reasignar_SoloRecepcion()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var cita = await CitaPendienteAsync(paciente, d[0], servicio, Local(12, 1, 12));
        var dentista = await _f.ClienteConRolAsync(Roles.Dentista);

        Assert.Equal(HttpStatusCode.Forbidden, (await dentista.PatchAsJsonAsync($"/api/Citas/{cita}/dentista", new { dentistaId = d[1], motivo = "x" })).StatusCode);
    }

    // ── Baja de un dentista (3.5) ────────────────────────
    [Fact]
    public async Task DentistaInactivo_ListaSusCitasFuturasActivas()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var futura = await CitaPendienteAsync(paciente, d[0], servicio, Local(12, 2, 9));
        var pasada = await CitaPendienteAsync(paciente, d[0], servicio, Local(11, 30, 8));
        var deOtro = await CitaPendienteAsync(paciente, d[1], servicio, Local(12, 2, 10));
        await _f.ConDbAsync(db => db.Dentistas.Where(x => x.Id == d[0]).ExecuteUpdateAsync(x => x.SetProperty(y => y.Activo, false)));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var ids = (await recepcion.GetFromJsonAsync<JsonElement>("/api/Citas?dentistaInactivo=true")).EnumerateArray().Select(c => c.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(futura, ids);
        Assert.DoesNotContain(pasada, ids);
        Assert.DoesNotContain(deOtro, ids);
    }
}

// Concurrencia con asignación (diseño, 10): 2 dentistas y 10 solicitudes simultáneas → 2 ganan
public class ConcurrenciaAsignacionTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _f;

    public ConcurrenciaAsignacionTests(DentissaFactory f) => _f = f;

    [Fact]
    public async Task DosDentistas_DiezSimultaneas_DosGanan_UnaPorDentista()
    {
        var (servicio, d, paciente) = await DatosAsignacion.ServicioConDentistasAsync(_f, 2);
        var clientes = new List<HttpClient>();
        for (var i = 0; i < 10; i++) clientes.Add(await _f.ClienteConRolAsync(Roles.Asistente));
        var fecha = ConcurrenciaReservasTests.FechaLaborable(9);
        using var barrera = new Barrier(10);

        var respuestas = await Task.WhenAll(clientes.Select(c => Task.Run(async () =>
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            return await c.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, servicioId = servicio, fecha, hora = "13:00:00" });
        })));

        Assert.Equal(2, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(8, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var asignados = await _f.ConDbAsync(db => db.Citas.Where(c => c.ServicioId == servicio).Select(c => c.DentistaId).OrderBy(x => x).ToListAsync());
        Assert.Equal(d, asignados);
    }
}
