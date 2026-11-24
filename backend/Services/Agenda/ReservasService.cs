using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using backend.Entities;
using backend.Entities.Configuraciones;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Services.Agenda;

/// <summary>Datos de una cita nueva. Sin dentista, la reserva lo asigna (diseño, 3.3); con él, debe ser elegible.</summary>
public sealed record SolicitudReserva(
    int PacienteId,
    int? DentistaId,
    int ServicioId,
    DateOnly Fecha,
    TimeOnly Hora,
    CanalReserva Canal,
    string Origen,
    string DetalleHistorial,
    string? Motivo = null,
    decimal? CostoExtra = null,
    string? MotivoExtra = null,
    bool ConfirmarAlCrear = false);

/// <summary>El dentista indicado no está activo o no atiende el servicio (400).</summary>
public class DentistaNoElegibleException : ArgumentException
{
    public DentistaNoElegibleException() : base("El dentista no está activo o no atiende ese servicio") { }
}

/// <summary>La hora pedida no es un inicio válido del horario de esa fecha para la duración del servicio.</summary>
public class HoraFueraDeHorarioException : InvalidOperationException
{
    public HoraFueraDeHorarioException() : base("La hora no corresponde a un horario de la clínica") { }
}

/// <summary>La regla de anticipación, horizonte o pasado rechazó la reserva (diseño, 1.5 y 2.3).</summary>
public class ReservaRechazadaException : InvalidOperationException
{
    public MotivoRechazo Motivo { get; }

    public ReservaRechazadaException(MotivoRechazo motivo, string mensaje) : base(mensaje) => Motivo = motivo;
}

/// <summary>
/// Reservas (diseño de la agenda, sección 6): candado por fecha local, liberación de solicitudes vencidas, regla
/// de bloqueo y alta, todo en una transacción; reintentos acotados con contexto nuevo.
/// </summary>
public interface IReservasService
{
    Task<Cita> ReservarAsync(SolicitudReserva solicitud, CancellationToken ct = default);

    /// <summary>
    /// Ejecuta una operación que ocupa horarios con <see cref="DentissaDbContext"/> y transacción nuevos en cada
    /// intento. Reintenta 40P01 y 23P01 hasta 3 veces (25–150 ms); al agotarse lanza <see cref="HorarioOcupadoException"/>.
    /// </summary>
    Task<T> EjecutarReservaAsync<T>(Func<IServiceProvider, DentissaDbContext, CancellationToken, Task<T>> operacion, CancellationToken ct = default);
}

public class ReservasService : IReservasService
{
    public const int MaxIntentos = 3;
    public const string MotivoVencimiento = "Solicitud no confirmada a tiempo";

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _tiempo;
    private readonly ILogger<ReservasService> _log;

    public ReservasService(IServiceScopeFactory scopes, TimeProvider tiempo, ILogger<ReservasService> log)
    {
        _scopes = scopes;
        _tiempo = tiempo;
        _log = log;
    }

    /// <summary>
    /// Clave del candado transaccional de una fecha local: primeros 8 bytes (big-endian) del SHA-256 de
    /// "dentissa:agenda:yyyy-MM-dd". Determinista entre procesos y máquinas (no usa GetHashCode).
    /// </summary>
    public static long ClaveCandado(DateOnly fechaLocal) => ClaveCandado($"dentissa:agenda:{fechaLocal:yyyy-MM-dd}");

    public static long ClaveCandado(string texto) =>
        BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));

    /// <summary>Toma el candado de cada fecha, en orden ascendente (evita interbloqueos entre reagendas).</summary>
    public static async Task TomarCandadosAsync(DentissaDbContext db, IEnumerable<DateOnly> fechas, CancellationToken ct)
    {
        foreach (var fecha in fechas.Distinct().Order())
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [ClaveCandado(fecha)], ct);
    }

    public async Task<T> EjecutarReservaAsync<T>(Func<IServiceProvider, DentissaDbContext, CancellationToken, Task<T>> operacion, CancellationToken ct = default)
    {
        for (var intento = 1; ; intento++)
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
            try
            {
                await using var transaccion = await db.Database.BeginTransactionAsync(ct);
                var resultado = await operacion(scope.ServiceProvider, db, ct);
                await transaccion.CommitAsync(ct);
                return resultado;
            }
            catch (Exception ex) when (EsReintentable(ex))
            {
                if (intento >= MaxIntentos)
                {
                    _log.LogWarning("Reserva sin lugar tras {Intentos} intentos ({Codigo})", intento, ErrorDePostgres(ex)?.SqlState);
                    throw new HorarioOcupadoException(ex);
                }
                await Task.Delay(Random.Shared.Next(25, 151), ct);
            }
        }
    }

    public Task<Cita> ReservarAsync(SolicitudReserva s, CancellationToken ct = default) =>
        EjecutarReservaAsync(async (sp, db, token) =>
        {
            var calendario = sp.GetRequiredService<ICalendarioAtencion>();
            var historial = sp.GetRequiredService<ICitaHistorialService>();
            var actor = sp.GetRequiredService<IActorActual>();

            var servicio = await db.Servicios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == s.ServicioId && x.Activo, token)
                ?? throw new ServicioNoDisponibleException();
            var config = await db.ConfiguracionAgenda.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, token)
                ?? throw new AgendaNoConfiguradaException();
            var horario = await calendario.ObtenerHorarioAsync(token);

            var opcion = horario.InicioPosibles(s.Fecha, config.DuracionSlotMinutos, servicio.DuracionSlots)
                .Where(o => o.Hora == s.Hora).ToList();
            if (opcion.Count == 0)
                throw new HoraFueraDeHorarioException();
            var (_, inicio, fin) = opcion[0];

            // Dentistas elegibles para el servicio (activos y ligados; sin ligados, cualquiera activo con aviso)
            var elegibles = await sp.GetRequiredService<IDisponibilidadService>().DentistasElegiblesAsync(servicio, token);
            List<int> candidatos;
            if (s.DentistaId is { } pedido)
                candidatos = elegibles.Contains(pedido) ? [pedido] : throw new DentistaNoElegibleException();
            else
                candidatos = elegibles;

            // 1. Candado de la fecha local: las reservas del mismo día se ordenan una tras otra
            await TomarCandadosAsync(db, [s.Fecha], token);
            // El instante se toma después del candado: la espera no debe adelantar el reloj de la regla
            var ahora = _tiempo.GetUtcNow().UtcDateTime;

            // 2. Regla de bloqueo y ocupación; una pendiente vencida no ocupa (la restricción de exclusión queda como respaldo)
            var regla = ReglaBloqueo.CalcularBloqueo(ahora, inicio, s.Canal, new RelojAtencion(horario), await calendario.ObtenerParametrosAsync(token));
            if (!regla.Aceptada)
                throw new ReservaRechazadaException(regla.Motivo!.Value, MensajeRechazo(regla.Motivo.Value, s.Canal));

            // 3. Asignación entre los candidatos libres (3.3)
            var libres = new List<int>();
            foreach (var id in candidatos)
                if (!await OcupadoAsync(db, id, inicio, fin, ahora, token))
                    libres.Add(id);
            if (libres.Count == 0)
                throw s.DentistaId is null ? new HorarioOcupadoException(HorarioOcupadoException.MensajeSinDentistas) : new HorarioOcupadoException();
            var dentista = libres.Count == 1 ? libres[0] : await ElegirDentistaAsync(db, libres, s.PacienteId, s.Fecha, horario, ahora, token);

            // 3. Liberación de las vencidas que se traslapan (6.1), a nombre del sistema. El id de la cita nueva se
            // reserva antes para que la bitácora diga qué reserva la provocó.
            int? idReservado = null;
            if (await HayVencidasAsync(db, [dentista], inicio, fin, ahora, token))
            {
                idReservado = (int)await db.Database
                    .SqlQueryRaw<long>("SELECT nextval(pg_get_serial_sequence('citas', 'id')) AS \"Value\"")
                    .SingleAsync(token);
                await LiberarVencidasAsync(db, historial, sp.GetRequiredService<ContextoAuditoria>(), [dentista], inicio, fin, ahora,
                    new { citaId = idReservado, usuarioId = actor.UsuarioId }, token);
            }

            // 4. Alta
            var oferta = await db.Ofertas.AsNoTracking()
                .Where(o => o.ServicioId == s.ServicioId && o.Activo && o.FechaInicio <= s.Fecha && o.FechaFin >= s.Fecha)
                .OrderByDescending(o => o.Descuento)
                .FirstOrDefaultAsync(token);
            var descuento = oferta?.Descuento ?? 0;
            var extra = s.CostoExtra ?? 0;
            var cita = new Cita
            {
                Id                = idReservado ?? 0,
                PacienteId        = s.PacienteId,
                DentistaId        = dentista,
                ServicioId        = s.ServicioId,
                OfertaId          = oferta?.Id,
                Inicio            = inicio,
                Fin               = fin,
                Estado            = EstadosCita.Pendiente,
                Origen            = s.Origen,
                CreadoPor         = actor.UsuarioId,
                Motivo            = s.Motivo,
                CostoBase         = servicio.Precio,
                DescuentoAplicado = descuento,
                CostoExtra        = extra,
                MotivoExtra       = s.MotivoExtra,
                CostoTotal        = CostosCita.Total(servicio.Precio, descuento, extra),
                BloqueoExpiraEn   = regla.BloqueoExpiraEn,
                PacientePresente  = s.Canal == CanalReserva.PacientePresente,
            };
            // Recepción puede crearla ya confirmada (decisión 6; sesiones de tratamiento, decisión 9)
            if (s.ConfirmarAlCrear)
            {
                cita.Estado            = EstadosCita.Confirmada;
                cita.ConfirmadaEn      = ahora;
                cita.ConfirmadaPor     = actor.UsuarioId;
                cita.ConfirmacionCanal = CanalesCitaHistorial.Panel;
            }
            db.Citas.Add(cita);
            historial.Registrar(cita, AccionesCitaHistorial.Creacion, s.DetalleHistorial,
                cita.PacientePresente || s.ConfirmarAlCrear ? new { pacientePresente = cita.PacientePresente, estado = cita.Estado } : null);
            if (s.ConfirmarAlCrear)
                historial.Registrar(cita, AccionesCitaHistorial.Confirmacion, "Confirmada al crearla en recepción");
            await db.SaveChangesAsync(token);
            return cita;
        }, ct);

    /// <summary>
    /// Orden determinista entre varios dentistas libres (diseño, 3.3): continuidad con el paciente, menor carga del
    /// día, menor carga de la semana (lunes a domingo, fecha local) e id menor.
    /// </summary>
    public static async Task<int> ElegirDentistaAsync(
        DentissaDbContext db, IReadOnlyCollection<int> libres, int pacienteId, DateOnly fecha, HorarioAtencion horario, DateTime ahora, CancellationToken ct)
    {
        var continuidad = await db.Citas.AsNoTracking()
            .Where(c => c.PacienteId == pacienteId && libres.Contains(c.DentistaId)
                        && (c.Estado == EstadosCita.Completada || c.Estado == EstadosCita.Confirmada || c.Estado == EstadosCita.Pendiente))
            .Select(c => c.DentistaId).Distinct().ToListAsync(ct);

        var lunes = fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
        var (diaDesde, diaHasta) = (horario.AUtc(fecha, TimeOnly.MinValue), horario.AUtc(fecha.AddDays(1), TimeOnly.MinValue));
        var (semDesde, semHasta) = (horario.AUtc(lunes, TimeOnly.MinValue), horario.AUtc(lunes.AddDays(7), TimeOnly.MinValue));
        var activas = await db.Citas.AsNoTracking()
            .Where(c => libres.Contains(c.DentistaId) && c.Inicio >= semDesde && c.Inicio < semHasta
                        && EstadosCita.Activos.Contains(c.Estado)
                        && !(c.Estado == EstadosCita.Pendiente && c.BloqueoExpiraEn != null && c.BloqueoExpiraEn <= ahora))
            .Select(c => new { c.DentistaId, c.Inicio }).ToListAsync(ct);

        return libres
            .OrderBy(id => continuidad.Contains(id) ? 0 : 1)
            .ThenBy(id => activas.Count(c => c.DentistaId == id && c.Inicio >= diaDesde && c.Inicio < diaHasta))
            .ThenBy(id => activas.Count(c => c.DentistaId == id))
            .ThenBy(id => id)
            .First();
    }

    private static IQueryable<Cita> Vencidas(DentissaDbContext db, IReadOnlyCollection<int> dentistas, DateTime inicio, DateTime fin, DateTime ahora, int? excluir = null) =>
        db.Citas.Where(c => dentistas.Contains(c.DentistaId) && c.Inicio < fin && inicio < c.Fin && c.Id != excluir
                            && c.Estado == EstadosCita.Pendiente && c.BloqueoExpiraEn != null && c.BloqueoExpiraEn <= ahora);

    public static Task<bool> HayVencidasAsync(DentissaDbContext db, IReadOnlyCollection<int> dentistas, DateTime inicio, DateTime fin, DateTime ahora, CancellationToken ct, int? excluir = null) =>
        Vencidas(db, dentistas, inicio, fin, ahora, excluir).AnyAsync(ct);

    /// <summary>¿El dentista tiene otra cita que ocupa [inicio, fin)? Una pendiente vencida no ocupa.</summary>
    public static Task<bool> OcupadoAsync(DentissaDbContext db, int dentista, DateTime inicio, DateTime fin, DateTime ahora, CancellationToken ct, int? excluir = null) =>
        db.Citas.AnyAsync(c => c.DentistaId == dentista && c.Inicio < fin && inicio < c.Fin && c.Id != excluir
                               && EstadosCita.Activos.Contains(c.Estado)
                               && !(c.Estado == EstadosCita.Pendiente && c.BloqueoExpiraEn != null && c.BloqueoExpiraEn <= ahora), ct);

    /// <summary>
    /// Cancela las pendientes con bloqueo vencido que se traslapan con [inicio, fin) para los dentistas dados
    /// (diseño, 6.1). Historial "vencimiento" con canal "sistema"; bitácora con actor "sistema", sin usuario, y
    /// <paramref name="provocadoPor"/> (cita y cuenta que la desencadenaron). El aviso al paciente llega con el commit 10.
    /// </summary>
    public static async Task<int> LiberarVencidasAsync(
        DentissaDbContext db, ICitaHistorialService historial, ContextoAuditoria auditoria, IReadOnlyCollection<int> dentistas,
        DateTime inicio, DateTime fin, DateTime ahora, object? provocadoPor, CancellationToken ct, int? excluir = null)
    {
        var vencidas = await Vencidas(db, dentistas, inicio, fin, ahora, excluir).ToListAsync(ct);
        foreach (var cita in vencidas)
        {
            cita.Estado = EstadosCita.Cancelada;
            cita.MotivoCancelacion = MotivoVencimiento;
            cita.CanceladaEn = ahora;
            historial.Registrar(cita, AccionesCitaHistorial.Vencimiento, MotivoVencimiento, canal: CitaHistorialService.CanalSistema);
        }
        if (vencidas.Count > 0)
            using (auditoria.ComoSistema(provocadoPor))
                await db.SaveChangesAsync(ct);
        return vencidas.Count;
    }

    public static string MensajeRechazo(MotivoRechazo motivo, CanalReserva canal) => motivo switch
    {
        MotivoRechazo.EnElPasado => "Ese horario ya pasó.",
        MotivoRechazo.FueraDeHorizonte => "Esa fecha todavía no está abierta para citas.",
        _ when canal == CanalReserva.Recepcion => "Ese horario empieza demasiado pronto para agendarlo.",
        _ => "Ese horario está muy próximo para solicitarlo en línea. Llámanos para agendar.",
    };

    private static bool EsReintentable(Exception ex) => ErrorDePostgres(ex) is { } pg &&
        (pg.SqlState == PostgresErrorCodes.DeadlockDetected
         || (pg.SqlState == PostgresErrorCodes.ExclusionViolation && pg.ConstraintName == CitaConfiguracion.RestriccionSinTraslape));

    private static PostgresException? ErrorDePostgres(Exception? ex)
    {
        for (; ex != null; ex = ex.InnerException)
            if (ex is PostgresException pg) return pg;
        return null;
    }
}

/// <summary>Costo total de una cita: base menos el descuento en porcentaje, más el extra; a 2 decimales.</summary>
public static class CostosCita
{
    public static decimal Total(decimal costoBase, decimal descuento, decimal extra)
        => Math.Round(costoBase - (costoBase * descuento / 100) + extra, 2, MidpointRounding.AwayFromZero);
}
