using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Repositories;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace backend.Tests;

// Fase 2, commit 6 (diseño de la agenda, sección 6): reservas con candado, liberación de vencidas y reintentos.
public class ClaveCandadoTests
{
    // Valores calculados fuera de .NET (Node: SHA-256, primeros 8 bytes big-endian con signo).
    // Si cambian, dos versiones de la API dejarían de compartir el candado de la misma fecha.
    [Theory]
    [InlineData("2026-11-30", 5490556455322635136L)]
    [InlineData("2026-12-01", -1262434137403422858L)]
    [InlineData("2031-02-04", -3403644432836427636L)]
    public void Clave_EsDeterminista_YCoincideConLaReferencia(string fecha, long esperada)
    {
        Assert.Equal(esperada, ReservasService.ClaveCandado(DateOnly.Parse(fecha)));
    }
}

public class ReservasTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public ReservasTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(FabricaDisponibilidad.SabadoNoche);
    }

    // Hora local de la clínica (America/Mexico_City, UTC−6 todo el año) a UTC
    private static DateTimeOffset Local(int anio, int mes, int dia, int hora, int minuto = 0) =>
        new(anio, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private async Task<Cita> ReservarAsync(SolicitudReserva s)
    {
        using var scope = _f.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IReservasService>().ReservarAsync(s);
    }

    private async Task CerrarAsync(DateOnly fecha, TimeOnly? desde = null, TimeOnly? hasta = null) => await _f.ConDbAsync(async db =>
    {
        if (await db.AgendaCierres.AnyAsync(c => c.Fecha == fecha && c.HoraInicio == desde)) return 0;
        db.AgendaCierres.Add(new AgendaCierre
        {
            ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = fecha, HoraInicio = desde, HoraFin = hasta, Motivo = "Prueba"
        });
        return await db.SaveChangesAsync();
    });

    // Tabla 2.5 de punta a punta: reloj fijo en "creado", reserva real con la base y sus cierres.
    // Columnas: creado (local), cita (local), canal, cierre, motivo de rechazo o vencimiento esperado (local).
    public static IEnumerable<object?[]> Tabla25() =>
    [
        [1,  "2026-11-28 20:00", "2026-11-30 08:00", CanalReserva.EnLinea,   null,       MotivoRechazo.AnticipacionInsuficiente, null],
        [2,  "2026-11-28 20:00", "2026-11-30 12:00", CanalReserva.EnLinea,   null,       null, "2026-11-30 09:00"],
        [3,  "2026-12-01 08:00", "2026-12-01 15:00", CanalReserva.EnLinea,   null,       null, "2026-12-01 12:00"],
        [4,  "2026-12-01 08:00", "2026-12-01 10:00", CanalReserva.EnLinea,   null,       MotivoRechazo.AnticipacionInsuficiente, null],
        [5,  "2026-12-01 09:00", "2026-12-01 13:00", CanalReserva.EnLinea,   null,       null, "2026-12-01 10:00"],
        [6,  "2026-12-01 11:30", "2026-12-01 15:00", CanalReserva.EnLinea,   null,       MotivoRechazo.AnticipacionInsuficiente, null],
        [7,  "2026-12-05 09:00", "2026-12-07 10:00", CanalReserva.EnLinea,   null,       null, "2026-12-07 08:00"],
        [8,  "2026-12-04 15:30", "2026-12-07 09:00", CanalReserva.EnLinea,   null,       null, "2026-12-05 15:00"],
        [9,  "2026-11-30 08:00", "2026-12-14 08:00", CanalReserva.EnLinea,   null,       null, "2026-12-01 16:00"],
        [10, "2026-12-24 10:00", "2026-12-26 10:00", CanalReserva.EnLinea,   "total",    null, "2026-12-26 08:00"],
        [11, "2026-12-24 15:00", "2026-12-26 09:00", CanalReserva.EnLinea,   "total",    MotivoRechazo.AnticipacionInsuficiente, null],
        [12, "2026-12-02 08:00", "2026-12-03 09:00", CanalReserva.EnLinea,   "parcial",  null, "2026-12-02 10:00"],
        [13, "2026-12-01 14:00", "2026-12-01 15:00", CanalReserva.Recepcion, null,       null, null],
        [14, "2026-12-01 14:30", "2026-12-01 15:00", CanalReserva.Recepcion, null,       MotivoRechazo.AnticipacionInsuficiente, null],
        [15, "2026-11-30 10:00", "2027-02-02 08:00", CanalReserva.EnLinea,   null,       MotivoRechazo.FueraDeHorizonte, null],
    ];

    private static DateTimeOffset Parsear(string local)
    {
        var dt = DateTime.ParseExact(local, "yyyy-MM-dd HH:mm", null);
        return Local(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute);
    }

    [Theory]
    [MemberData(nameof(Tabla25))]
    public async Task Tabla25_DePuntaAPunta(int fila, string creado, string inicio, CanalReserva canal, string? cierre, MotivoRechazo? rechazo, string? vence)
    {
        // Cierres de las filas 10–12 (vie 25-dic completo; mié 2-dic de 12:00 a 16:00)
        if (cierre == "total") await CerrarAsync(new DateOnly(2026, 12, 25));
        if (cierre == "parcial") await CerrarAsync(new DateOnly(2026, 12, 2), new TimeOnly(12, 0), new TimeOnly(16, 0));
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        var cita = Parsear(inicio);
        _f.Reloj.Fijar(Parsear(creado));
        var solicitud = new SolicitudReserva(
            paciente, dentista, servicio, DateOnly.FromDateTime(cita.DateTime), TimeOnly.FromDateTime(cita.DateTime), canal,
            canal == CanalReserva.Recepcion ? OrigenesCita.Recepcion : OrigenesCita.Web, $"Fila {fila}");

        if (rechazo is { } motivo)
        {
            var ex = await Assert.ThrowsAsync<ReservaRechazadaException>(() => ReservarAsync(solicitud));
            Assert.Equal(motivo, ex.Motivo);
            Assert.False(await _f.ConDbAsync(db => db.Citas.AnyAsync(c => c.DentistaId == dentista)));
            return;
        }

        var reservada = await ReservarAsync(solicitud);
        var guardada = await _f.ConDbAsync(db => db.Citas.AsNoTracking().SingleAsync(c => c.Id == reservada.Id));
        Assert.Equal(cita.UtcDateTime, guardada.Inicio);
        Assert.Equal(Parsear(creado).UtcDateTime, guardada.CreadoEn);
        Assert.Equal(vence is null ? null : Parsear(vence).UtcDateTime, guardada.BloqueoExpiraEn);
    }

    // Pendiente en línea ya vencida en el horario que se pide: lunes 30-nov 12:00, venció a las 9:00
    private async Task<(int Dentista, int Servicio, int Paciente, int Vencida)> ConSolicitudAsync(DateTimeOffset bloqueoExpira)
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        var vieja = DentissaFactory.NuevaCita(paciente, dentista, servicio, Local(2026, 11, 30, 12).UtcDateTime);
        vieja.Origen = OrigenesCita.Web;
        vieja.BloqueoExpiraEn = bloqueoExpira.UtcDateTime;
        await _f.ConDbAsync(db => { db.Citas.Add(vieja); return db.SaveChangesAsync(); });
        return (dentista, servicio, paciente, vieja.Id);
    }

    [Fact]
    public async Task Panel_SobreUnaVencida_LaCancelaEnLaMismaTransaccion_YReserva()
    {
        var (dentista, servicio, paciente, vencida) = await ConSolicitudAsync(Local(2026, 11, 30, 9));
        _f.Reloj.Fijar(Local(2026, 11, 30, 10));
        var cuenta = await _f.CrearUsuarioAsync(Roles.Asistente);
        var recepcion = await _f.ClienteDeAsync(cuenta);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha = "2026-11-30", hora = "12:00:00" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var nueva = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var vieja = await _f.ConDbAsync(db => db.Citas.AsNoTracking().Include(c => c.Historial).SingleAsync(c => c.Id == vencida));
        Assert.Equal(EstadosCita.Cancelada, vieja.Estado);
        Assert.Equal(ReservasService.MotivoVencimiento, vieja.MotivoCancelacion);
        Assert.Equal(Local(2026, 11, 30, 10).UtcDateTime, vieja.CanceladaEn);
        var h = Assert.Single(vieja.Historial, x => x.Accion == AccionesCitaHistorial.Vencimiento);
        Assert.Equal(CitaHistorialService.CanalSistema, h.Canal);
        Assert.Null(h.UsuarioId);
        Assert.Equal(Local(2026, 11, 30, 10).UtcDateTime, h.CreadoEn);
        // Bitácora: el vencimiento va a nombre del sistema, sin usuario, con la cita y la cuenta que lo provocaron
        var fila = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .SingleAsync(a => a.Entidad == "citas" && a.EntidadId == vencida.ToString() && a.Accion == AccionesAuditoria.Modificar));
        Assert.Equal(ContextoAuditoria.ActorSistema, fila.Actor);
        Assert.Null(fila.UsuarioId);
        Assert.Null(fila.UsuarioCorreo);
        Assert.Null(fila.Ip);
        var provocado = JsonDocument.Parse(fila.ProvocadoPor!).RootElement;
        Assert.Equal(nueva, provocado.GetProperty("citaId").GetInt32());
        Assert.Equal(cuenta.Id, provocado.GetProperty("usuarioId").GetInt32());
        // El alta de la cita nueva sigue a nombre de la cuenta
        var alta = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .SingleAsync(a => a.Entidad == "citas" && a.EntidadId == nueva.ToString() && a.Accion == AccionesAuditoria.Crear));
        Assert.Equal(cuenta.Id, alta.UsuarioId);
        Assert.Null(alta.Actor);
        Assert.Null(alta.ProvocadoPor);
        Assert.Equal(1, await _f.ConDbAsync(db => db.Citas.CountAsync(c => c.DentistaId == dentista && EstadosCita.Activos.Contains(c.Estado))));
    }

    [Fact]
    public async Task Panel_SobreUnaVigente_409SinTocarla()
    {
        var (dentista, servicio, paciente, vigente) = await ConSolicitudAsync(Local(2026, 11, 30, 11));
        _f.Reloj.Fijar(Local(2026, 11, 30, 10));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha = "2026-11-30", hora = "12:00:00" });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        var cuerpo = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("HORARIO_OCUPADO", cuerpo.GetProperty("codigo").GetString());
        Assert.Equal(HorarioOcupadoException.Mensaje, cuerpo.GetProperty("mensaje").GetString());
        Assert.Equal(EstadosCita.Pendiente, (await _f.ConDbAsync(db => db.Citas.AsNoTracking().SingleAsync(c => c.Id == vigente))).Estado);
    }

    [Theory]
    [InlineData("2026-12-01", "15:00:00", "ANTICIPACION_INSUFICIENTE")] // fila 14: 30 min de reloj
    [InlineData("2026-12-01", "12:00:00", "HORARIO_PASADO")]
    [InlineData("2027-02-02", "08:00:00", "FUERA_DE_HORIZONTE")]
    [InlineData("2026-12-01", "15:30:00", "HORA_FUERA_DE_HORARIO")]
    [InlineData("2026-12-01", "11:00:00", "HORA_FUERA_DE_HORARIO")] // descanso
    public async Task Panel_ReglasDeRecepcion_400ConCodigo(string fecha, string hora, string codigo)
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        _f.Reloj.Fijar(Local(2026, 12, 1, 14, 30));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha, hora });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(codigo, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Panel_Fila13_RecepcionAceptaSinVencimiento()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        _f.Reloj.Fijar(Local(2026, 12, 1, 14));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha = "2026-12-01", hora = "15:00:00" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cita = await _f.ConDbAsync(db => db.Citas.AsNoTracking().SingleAsync(c => c.DentistaId == dentista));
        Assert.Null(cita.BloqueoExpiraEn);
        Assert.Equal(OrigenesCita.Recepcion, cita.Origen);
    }
}

// Reintentos acotados (6.3) con errores reales de PostgreSQL lanzados desde la base
public class ReintentosReservaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _f;

    public ReintentosReservaTests(DentissaFactory f) => _f = f;

    private Task<T> EjecutarAsync<T>(Func<IServiceProvider, DentissaDbContext, CancellationToken, Task<T>> operacion)
    {
        using var scope = _f.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IReservasService>().EjecutarReservaAsync(operacion);
    }

    // Los códigos son constantes de la prueba, no entrada externa
    private static Task Lanzar(DentissaDbContext db, string sqlState, string? restriccion = null)
    {
        var sql = "DO $$ BEGIN RAISE EXCEPTION 'prueba' USING ERRCODE = '" + sqlState + "'"
                  + (restriccion is null ? "" : ", CONSTRAINT = '" + restriccion + "'") + "; END $$";
        return db.Database.ExecuteSqlRawAsync(sql);
    }

    [Fact]
    public async Task Interbloqueo_SeReintentaConContextoNuevo_YTerminaBien()
    {
        var contextos = new List<DentissaDbContext>();

        var resultado = await EjecutarAsync(async (_, db, _) =>
        {
            contextos.Add(db);
            if (contextos.Count < 3) await Lanzar(db, PostgresErrorCodes.DeadlockDetected);
            return "listo";
        });

        Assert.Equal("listo", resultado);
        Assert.Equal(3, contextos.Count);
        Assert.Equal(3, contextos.Distinct().Count());
    }

    [Fact]
    public async Task TraslapeSiempre_TresIntentos_YLuegoHorarioOcupado()
    {
        var intentos = 0;

        await Assert.ThrowsAsync<HorarioOcupadoException>(() => EjecutarAsync<int>(async (_, db, _) =>
        {
            intentos++;
            await Lanzar(db, PostgresErrorCodes.ExclusionViolation, "citas_sin_traslape_dentista");
            return 0;
        }));

        Assert.Equal(ReservasService.MaxIntentos, intentos);
    }

    [Fact]
    public async Task OtroError_NoSeReintenta()
    {
        var intentos = 0;

        await Assert.ThrowsAnyAsync<Exception>(() => EjecutarAsync<int>(async (_, db, _) =>
        {
            intentos++;
            await Lanzar(db, PostgresErrorCodes.UniqueViolation);
            return 0;
        }));

        Assert.Equal(1, intentos);
    }

    [Fact]
    public async Task UnIntentoFallido_NoDejaCambios()
    {
        var (paciente, dentista, servicio) = await _f.CrearParticipantesAsync();
        var intentos = 0;

        await EjecutarAsync(async (_, db, ct) =>
        {
            intentos++;
            db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentista, servicio, new DateTime(2031, 3, 3, 14 + intentos, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync(ct);
            if (intentos == 1) await Lanzar(db, PostgresErrorCodes.DeadlockDetected);
            return 0;
        });

        // Solo queda la cita del segundo intento: el primero se revirtió con su transacción
        var citas = await _f.ConDbAsync(db => db.Citas.Where(c => c.DentistaId == dentista).Select(c => c.Inicio).ToListAsync());
        Assert.Equal([new DateTime(2031, 3, 3, 16, 0, 0, DateTimeKind.Utc)], citas);
    }
}

// Concurrencia con el reloj del sistema (diseño, 10): fecha de la próxima semana, lunes a sábado
public class ConcurrenciaReservasTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _f;

    public ConcurrenciaReservasTests(DentissaFactory f) => _f = f;

    public static string FechaLaborable(int diasDesdeHoy = 7)
    {
        var fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(diasDesdeHoy);
        return (fecha.DayOfWeek == DayOfWeek.Sunday ? fecha.AddDays(1) : fecha).ToString("yyyy-MM-dd");
    }

    [Fact]
    public async Task DiezSimultaneas_UnaGana_Y9Reciben409_SinErrores500()
    {
        var (paciente, dentista, servicio) = await _f.CrearParticipantesAsync();
        var clientes = new List<HttpClient>();
        for (var i = 0; i < 10; i++) clientes.Add(await _f.ClienteConRolAsync(Roles.Asistente));
        var fecha = FechaLaborable();
        using var barrera = new Barrier(10);

        var respuestas = await Task.WhenAll(clientes.Select(c => Task.Run(async () =>
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            return await c.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha, hora = "12:00:00" });
        })));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(9, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await _f.ConDbAsync(db => db.Citas.CountAsync(c => c.DentistaId == dentista)));
    }

    [Fact]
    public async Task DiezSimultaneas_DistintosHorariosDelMismoDia_TodasGanan()
    {
        // El candado ordena el día, pero no rechaza horarios distintos
        var (paciente, dentista, servicio) = await _f.CrearParticipantesAsync();
        var (_, otro, _) = await _f.CrearParticipantesAsync();
        var fecha = FechaLaborable(8);
        string[] horas = ["08:00:00", "09:00:00", "10:00:00", "12:00:00", "13:00:00"];
        var pedidos = horas.SelectMany(h => new[] { (dentista, h), (otro, h) }).ToList();
        var clientes = new List<HttpClient>();
        foreach (var _ in pedidos) clientes.Add(await _f.ClienteConRolAsync(Roles.Asistente));
        using var barrera = new Barrier(pedidos.Count);

        var respuestas = await Task.WhenAll(pedidos.Select((p, i) => Task.Run(async () =>
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            return await clientes[i].PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = p.Item1, servicioId = servicio, fecha, hora = p.Item2 });
        })));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
    }
}
