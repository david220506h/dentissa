using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

/// <summary>Un horario que se puede reservar: hora local, intervalo UTC y dentistas libres (nunca sale en público).</summary>
public sealed record HorarioDisponible(DateOnly Fecha, TimeOnly Hora, DateTime InicioUtc, DateTime FinUtc, IReadOnlyList<int> DentistasLibres);

/// <summary>El servicio no existe, está eliminado o inactivo.</summary>
public class ServicioNoDisponibleException : InvalidOperationException
{
    public ServicioNoDisponibleException() : base("El servicio no está disponible.") { }
}

/// <summary>Rango de fechas inválido (invertido o más largo de lo permitido).</summary>
public class RangoInvalidoException : ArgumentException
{
    public RangoInvalidoException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Motor único de disponibilidad (diseño de la agenda, sección 1) para el sitio, el portal, el panel y la
/// validación de cada reserva. Lo que muestra es orientativo: la reserva vuelve a calcular en su transacción.
/// </summary>
public interface IDisponibilidadService
{
    Task<IReadOnlyList<HorarioDisponible>> CalcularAsync(
        DateOnly desde, DateOnly hasta, int servicioId, CanalReserva canal, int? dentistaId = null, int maxDias = 92, CancellationToken ct = default);
}

public class DisponibilidadService : IDisponibilidadService
{
    public const string TipoAvisoConfiguracion = "aviso_configuracion";

    private readonly DentissaDbContext _db;
    private readonly ICalendarioAtencion _calendario;
    private readonly TimeProvider _tiempo;

    public DisponibilidadService(DentissaDbContext db, ICalendarioAtencion calendario, TimeProvider tiempo)
    {
        _db = db;
        _calendario = calendario;
        _tiempo = tiempo;
    }

    public async Task<IReadOnlyList<HorarioDisponible>> CalcularAsync(
        DateOnly desde, DateOnly hasta, int servicioId, CanalReserva canal, int? dentistaId = null, int maxDias = 92, CancellationToken ct = default)
    {
        if (hasta < desde)
            throw new RangoInvalidoException("La fecha final no puede ser anterior a la inicial");
        if (hasta.DayNumber - desde.DayNumber + 1 > maxDias)
            throw new RangoInvalidoException($"El rango máximo es de {maxDias} días");

        var servicio = await _db.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == servicioId && s.Activo, ct)
            ?? throw new ServicioNoDisponibleException();
        var config = await _db.ConfiguracionAgenda.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, ct)
            ?? throw new AgendaNoConfiguradaException();
        var horario = await _calendario.ObtenerHorarioAsync(ct);
        var parametros = await _calendario.ObtenerParametrosAsync(ct);
        var reloj = new RelojAtencion(horario);
        var ahora = _tiempo.GetUtcNow().UtcDateTime;

        var elegibles = await DentistasElegiblesAsync(servicio, ct);
        if (dentistaId is { } elegido)
            elegibles = elegibles.Where(id => id == elegido).ToList();
        if (elegibles.Count == 0)
            return Array.Empty<HorarioDisponible>();

        // Una consulta por rango; el resto en memoria (diseño, 1.4)
        var desdeUtc = horario.AUtc(desde, TimeOnly.MinValue);
        var hastaUtc = horario.AUtc(hasta.AddDays(1), TimeOnly.MinValue);
        var ocupacion = await _db.Citas.AsNoTracking()
            .Where(c => elegibles.Contains(c.DentistaId) && c.Inicio < hastaUtc && c.Fin > desdeUtc)
            .Where(c => c.Estado == EstadosCita.Confirmada
                        || (c.Estado == EstadosCita.Pendiente && (c.BloqueoExpiraEn == null || c.BloqueoExpiraEn > ahora)))
            .Select(c => new { c.DentistaId, c.Inicio, c.Fin })
            .ToListAsync(ct);

        var resultado = new List<HorarioDisponible>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            foreach (var (hora, inicio, fin) in horario.InicioPosibles(fecha, config.DuracionSlotMinutos, servicio.DuracionSlots))
            {
                if (!ReglaBloqueo.CalcularBloqueo(ahora, inicio, canal, reloj, parametros).Aceptada) continue;
                var libres = elegibles
                    .Where(id => !ocupacion.Any(c => c.DentistaId == id && c.Inicio < fin && inicio < c.Fin))
                    .ToList();
                if (libres.Count > 0)
                    resultado.Add(new HorarioDisponible(fecha, hora, inicio, fin, libres));
            }
        return resultado;
    }

    /// <summary>
    /// Activos y ligados al servicio. Si el servicio no tiene ningún dentista ligado, lo atiende cualquier dentista
    /// activo y se avisa a la administración (decisión 4).
    /// </summary>
    private async Task<List<int>> DentistasElegiblesAsync(Servicio servicio, CancellationToken ct)
    {
        var ligados = await _db.DentistaServicios.AsNoTracking()
            .Where(ds => ds.ServicioId == servicio.Id)
            .Select(ds => new { ds.DentistaId, ds.Dentista.Activo })
            .ToListAsync(ct);
        if (ligados.Count > 0)
            return ligados.Where(l => l.Activo).Select(l => l.DentistaId).ToList();

        await AvisarServicioSinDentistasAsync(servicio, ct);
        return await _db.Dentistas.AsNoTracking().Where(d => d.Activo).Select(d => d.Id).ToListAsync(ct);
    }

    /// <summary>Una notificación interna sin leer por cuenta de administración y por servicio, sin duplicar.</summary>
    private async Task AvisarServicioSinDentistasAsync(Servicio servicio, CancellationToken ct)
    {
        var asunto = $"Servicio sin dentistas asignados (#{servicio.Id})";
        var admins = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == Roles.Admin && u.Activo)
            .Select(u => u.Id)
            .ToListAsync(ct);
        var yaAvisados = await _db.Notificaciones.AsNoTracking()
            .Where(n => n.Tipo == TipoAvisoConfiguracion && !n.Leida && n.Asunto == asunto && n.UsuarioId != null)
            .Select(n => n.UsuarioId!.Value)
            .ToListAsync(ct);
        var nuevos = admins.Except(yaAvisados).ToList();
        if (nuevos.Count == 0) return;

        var ahora = _tiempo.GetUtcNow().UtcDateTime;
        foreach (var admin in nuevos)
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = admin,
                Tipo = TipoAvisoConfiguracion,
                Canal = "interna",
                Asunto = asunto,
                CreadoEn = ahora,
                Mensaje = $"\"{servicio.Nombre}\" no tiene dentistas asignados: lo puede atender cualquier dentista activo. Asigna dentistas en Servicios.",
            });
        await _db.SaveChangesAsync(ct);
    }
}
