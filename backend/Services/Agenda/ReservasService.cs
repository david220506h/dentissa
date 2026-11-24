using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using backend.Entities;
using backend.Entities.Configuraciones;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Services.Agenda;

/// <summary>Datos de una cita nueva. El dentista lo indica recepción; la asignación automática es del commit 8.</summary>
public sealed record SolicitudReserva(
    int PacienteId,
    int DentistaId,
    int ServicioId,
    DateOnly Fecha,
    TimeOnly Hora,
    CanalReserva Canal,
    string Origen,
    string DetalleHistorial,
    string? Motivo = null,
    decimal? CostoExtra = null,
    string? MotivoExtra = null);

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

            // 1. Candado de la fecha local: las reservas del mismo día se ordenan una tras otra
            await TomarCandadosAsync(db, [s.Fecha], token);
            // El instante se toma después del candado: la espera no debe adelantar el reloj de la regla
            var ahora = _tiempo.GetUtcNow().UtcDateTime;

            // 2. Liberación de solicitudes vencidas que se traslapan, para los dentistas candidatos
            await LiberarVencidasAsync(db, historial, [s.DentistaId], inicio, fin, ahora, token);

            // 3. Regla de bloqueo y ocupación (la restricción de exclusión queda como respaldo)
            var regla = ReglaBloqueo.CalcularBloqueo(ahora, inicio, s.Canal, new RelojAtencion(horario), await calendario.ObtenerParametrosAsync(token));
            if (!regla.Aceptada)
                throw new ReservaRechazadaException(regla.Motivo!.Value, MensajeRechazo(regla.Motivo.Value, s.Canal));

            var ocupado = await db.Citas.AnyAsync(c => c.DentistaId == s.DentistaId && c.Inicio < fin && inicio < c.Fin
                                                       && EstadosCita.Activos.Contains(c.Estado), token);
            if (ocupado)
                throw new HorarioOcupadoException();

            // 4. Alta
            var oferta = await db.Ofertas.AsNoTracking()
                .Where(o => o.ServicioId == s.ServicioId && o.Activo && o.FechaInicio <= s.Fecha && o.FechaFin >= s.Fecha)
                .OrderByDescending(o => o.Descuento)
                .FirstOrDefaultAsync(token);
            var descuento = oferta?.Descuento ?? 0;
            var extra = s.CostoExtra ?? 0;
            var cita = new Cita
            {
                PacienteId        = s.PacienteId,
                DentistaId        = s.DentistaId,
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
            };
            db.Citas.Add(cita);
            historial.Registrar(cita, AccionesCitaHistorial.Creacion, s.DetalleHistorial);
            await db.SaveChangesAsync(token);
            return cita;
        }, ct);

    /// <summary>
    /// Cancela las pendientes con bloqueo vencido que se traslapan con [inicio, fin) para los dentistas dados
    /// (diseño, 6.1). Historial "vencimiento" con canal "sistema"; la bitácora la escribe el interceptor.
    /// El aviso al paciente por correo llega con la bandeja de salida (commit 10).
    /// </summary>
    public static async Task<int> LiberarVencidasAsync(
        DentissaDbContext db, ICitaHistorialService historial, IReadOnlyCollection<int> dentistas, DateTime inicio, DateTime fin, DateTime ahora, CancellationToken ct)
    {
        var vencidas = await db.Citas
            .Where(c => dentistas.Contains(c.DentistaId) && c.Inicio < fin && inicio < c.Fin
                        && c.Estado == EstadosCita.Pendiente && c.BloqueoExpiraEn != null && c.BloqueoExpiraEn <= ahora)
            .ToListAsync(ct);
        foreach (var cita in vencidas)
        {
            cita.Estado = EstadosCita.Cancelada;
            cita.MotivoCancelacion = MotivoVencimiento;
            cita.CanceladaEn = ahora;
            historial.Registrar(cita, AccionesCitaHistorial.Vencimiento, MotivoVencimiento, canal: CitaHistorialService.CanalSistema);
        }
        if (vencidas.Count > 0)
            await db.SaveChangesAsync(ct);
        return vencidas.Count;
    }

    private static string MensajeRechazo(MotivoRechazo motivo, CanalReserva canal) => motivo switch
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
