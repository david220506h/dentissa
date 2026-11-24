using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

public sealed record DiaDto(short DiaSemana, bool Laborable, TimeOnly? Apertura, TimeOnly? Cierre);
public sealed record DescansoDto(short? DiaSemana, TimeOnly Inicio, TimeOnly Fin);

/// <summary>Configuración editable de la agenda (diseño, 9.4). Minutos de atención salvo la anticipación de recepción.</summary>
public sealed record ConfiguracionAgendaDto(
    short DuracionSlotMinutos,
    short AnticipacionEnLineaAtencionMinutos,
    short AnticipacionRecepcionMinutos,
    short LiberacionAntesCitaAtencionMinutos,
    short VidaMaximaSolicitudAtencionMinutos,
    short HorizonteReservaDias,
    IReadOnlyList<DiaDto> Dias,
    IReadOnlyList<DescansoDto> Descansos);

public sealed record CierreDto(int Id, DateOnly Fecha, TimeOnly? HoraInicio, TimeOnly? HoraFin, string? Motivo);

/// <summary>Cita activa futura que no coincide con el horario (decisión 20): se lista, nunca se cancela sola.</summary>
public sealed record CitaFueraDeHorario(int Id, DateOnly Fecha, TimeOnly Hora, int DentistaId, string Estado);

public interface IConfiguracionAgendaService
{
    Task<ConfiguracionAgendaDto> ObtenerAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CitaFueraDeHorario>> GuardarAsync(ConfiguracionAgendaDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<CitaFueraDeHorario>> CitasFueraDeHorarioAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CierreDto>> CierresAsync(DateOnly desde, CancellationToken ct = default);
    Task<(CierreDto Cierre, IReadOnlyList<CitaFueraDeHorario> Afectadas)> CrearCierreAsync(DateOnly fecha, TimeOnly? inicio, TimeOnly? fin, string? motivo, CancellationToken ct = default);
    Task<bool> EliminarCierreAsync(int id, CancellationToken ct = default);
}

public class ConfiguracionAgendaService : IConfiguracionAgendaService
{
    private readonly DentissaDbContext _db;
    private readonly ICalendarioAtencion _calendario;
    private readonly IActorActual _actor;
    private readonly IRelojClinica _reloj;
    private readonly TimeProvider _tiempo;

    public ConfiguracionAgendaService(DentissaDbContext db, ICalendarioAtencion calendario, IActorActual actor, IRelojClinica reloj, TimeProvider tiempo)
    {
        _db = db;
        _calendario = calendario;
        _actor = actor;
        _reloj = reloj;
        _tiempo = tiempo;
    }

    private DateTime Ahora => _tiempo.GetUtcNow().UtcDateTime;

    private async Task<ConfiguracionAgenda> CargarAsync(CancellationToken ct) =>
        await _db.ConfiguracionAgenda.Include(c => c.Dias).Include(c => c.Descansos)
            .FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, ct)
        ?? throw new AgendaNoConfiguradaException();

    public async Task<ConfiguracionAgendaDto> ObtenerAsync(CancellationToken ct = default)
    {
        var c = await CargarAsync(ct);
        return new ConfiguracionAgendaDto(c.DuracionSlotMinutos, c.AnticipacionEnLineaAtencionMinutos, c.AnticipacionRecepcionMinutos,
            c.LiberacionAntesCitaAtencionMinutos, c.VidaMaximaSolicitudAtencionMinutos, c.HorizonteReservaDias,
            c.Dias.OrderBy(d => d.DiaSemana).Select(d => new DiaDto(d.DiaSemana, d.Laborable, d.HoraApertura, d.HoraCierre)).ToList(),
            c.Descansos.OrderBy(d => d.DiaSemana ?? 0).ThenBy(d => d.HoraInicio).Select(d => new DescansoDto(d.DiaSemana, d.HoraInicio, d.HoraFin)).ToList());
    }

    /// <summary>Valida en el backend (autoridad); las restricciones de la base quedan como respaldo.</summary>
    public static void Validar(ConfiguracionAgendaDto d)
    {
        if (d.DuracionSlotMinutos is < 15 or > 240) throw new ArgumentException("La duración de un horario va de 15 a 240 minutos");
        if (d.AnticipacionRecepcionMinutos < 0) throw new ArgumentException("La anticipación de recepción no puede ser negativa");
        if (d.LiberacionAntesCitaAtencionMinutos <= 0 || d.VidaMaximaSolicitudAtencionMinutos <= 0 || d.HorizonteReservaDias <= 0)
            throw new ArgumentException("La liberación, la vida máxima y el horizonte deben ser mayores que cero");
        if (d.AnticipacionEnLineaAtencionMinutos <= d.LiberacionAntesCitaAtencionMinutos)
            throw new ArgumentException("La anticipación en línea debe ser mayor que la liberación");
        if (d.HorizonteReservaDias > 365) throw new ArgumentException("El horizonte máximo es de 365 días");
        if (d.Dias.Select(x => x.DiaSemana).Distinct().Count() != d.Dias.Count || d.Dias.Any(x => x.DiaSemana is < 1 or > 7))
            throw new ArgumentException("Cada día de la semana (1 a 7) va una sola vez");
        foreach (var dia in d.Dias.Where(x => x.Laborable))
            if (dia.Apertura is not { } a || dia.Cierre is not { } c || c <= a)
                throw new ArgumentException("Un día laborable necesita apertura y cierre, con el cierre después de la apertura");
        if (!d.Dias.Any(x => x.Laborable)) throw new ArgumentException("Debe haber al menos un día laborable");
        foreach (var x in d.Descansos)
        {
            if (x.Fin <= x.Inicio) throw new ArgumentException("Un descanso termina después de empezar");
            if (x.DiaSemana is { } ds && ds is < 1 or > 7) throw new ArgumentException("Día de descanso inválido");
        }
    }

    public async Task<IReadOnlyList<CitaFueraDeHorario>> GuardarAsync(ConfiguracionAgendaDto dto, CancellationToken ct = default)
    {
        Validar(dto);
        var c = await CargarAsync(ct);
        c.DuracionSlotMinutos = dto.DuracionSlotMinutos;
        c.AnticipacionEnLineaAtencionMinutos = dto.AnticipacionEnLineaAtencionMinutos;
        c.AnticipacionRecepcionMinutos = dto.AnticipacionRecepcionMinutos;
        c.LiberacionAntesCitaAtencionMinutos = dto.LiberacionAntesCitaAtencionMinutos;
        c.VidaMaximaSolicitudAtencionMinutos = dto.VidaMaximaSolicitudAtencionMinutos;
        c.HorizonteReservaDias = dto.HorizonteReservaDias;
        c.ActualizadoEn = Ahora;
        c.ActualizadoPor = _actor.UsuarioId;

        // Días: se actualizan en su lugar (índice único por día); descansos: se reemplazan
        foreach (var dia in dto.Dias)
        {
            var actual = c.Dias.FirstOrDefault(x => x.DiaSemana == dia.DiaSemana);
            if (actual is null) c.Dias.Add(actual = new HorarioDia { DiaSemana = dia.DiaSemana });
            actual.Laborable = dia.Laborable;
            actual.HoraApertura = dia.Laborable ? dia.Apertura : null;
            actual.HoraCierre = dia.Laborable ? dia.Cierre : null;
        }
        foreach (var sobra in c.Dias.Where(x => dto.Dias.All(d => d.DiaSemana != x.DiaSemana)).ToList())
        {
            sobra.Laborable = false;
            sobra.HoraApertura = sobra.HoraCierre = null;
        }
        var nuevos = dto.Descansos.Select(x => (x.DiaSemana, x.Inicio, x.Fin)).ToHashSet();
        foreach (var d in c.Descansos.Where(x => !nuevos.Contains((x.DiaSemana, x.HoraInicio, x.HoraFin))).ToList())
            _db.HorarioDescansos.Remove(d);
        foreach (var n in nuevos.Where(n => !c.Descansos.Any(x => (x.DiaSemana, x.HoraInicio, x.HoraFin) == n)))
            c.Descansos.Add(new HorarioDescanso { DiaSemana = n.DiaSemana, HoraInicio = n.Inicio, HoraFin = n.Fin });

        await _db.SaveChangesAsync(ct);
        return await CitasFueraDeHorarioAsync(ct);
    }

    public async Task<IReadOnlyList<CitaFueraDeHorario>> CitasFueraDeHorarioAsync(CancellationToken ct = default)
    {
        var config = await _db.ConfiguracionAgenda.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, ct)
            ?? throw new AgendaNoConfiguradaException();
        var horario = await _calendario.ObtenerHorarioAsync(ct);
        var citas = await _db.Citas.AsNoTracking()
            .Where(x => x.Inicio >= Ahora && EstadosCita.Activos.Contains(x.Estado))
            .Select(x => new { x.Id, x.Inicio, x.Fin, x.DentistaId, x.Estado })
            .OrderBy(x => x.Inicio)
            .ToListAsync(ct);
        var fuera = new List<CitaFueraDeHorario>();
        foreach (var x in citas)
        {
            var (fecha, hora) = _reloj.ALocal(x.Inicio);
            var espacios = (int)Math.Max(1, Math.Round((x.Fin - x.Inicio).TotalMinutes / config.DuracionSlotMinutos));
            if (!horario.InicioPosibles(fecha, config.DuracionSlotMinutos, espacios).Any(o => o.InicioUtc == x.Inicio && o.FinUtc == x.Fin))
                fuera.Add(new CitaFueraDeHorario(x.Id, fecha, hora, x.DentistaId, x.Estado));
        }
        return fuera;
    }

    public async Task<IReadOnlyList<CierreDto>> CierresAsync(DateOnly desde, CancellationToken ct = default) =>
        await _db.AgendaCierres.AsNoTracking().Where(c => c.Fecha >= desde).OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
            .Select(c => new CierreDto(c.Id, c.Fecha, c.HoraInicio, c.HoraFin, c.Motivo)).ToListAsync(ct);

    public async Task<(CierreDto Cierre, IReadOnlyList<CitaFueraDeHorario> Afectadas)> CrearCierreAsync(
        DateOnly fecha, TimeOnly? inicio, TimeOnly? fin, string? motivo, CancellationToken ct = default)
    {
        if (fecha < _reloj.HoyLocal()) throw new ArgumentException("No se cierra una fecha pasada");
        if ((inicio is null) != (fin is null)) throw new ArgumentException("Un cierre parcial necesita hora de inicio y de fin");
        if (inicio is { } i && fin is { } f && f <= i) throw new ArgumentException("El cierre termina después de empezar");
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("El motivo del cierre es obligatorio");

        var cierre = new AgendaCierre
        {
            ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = fecha, HoraInicio = inicio, HoraFin = fin,
            Motivo = motivo.Trim(), CreadoEn = Ahora, CreadoPor = _actor.UsuarioId,
        };
        _db.AgendaCierres.Add(cierre);
        await _db.SaveChangesAsync(ct);

        // Citas que el cierre deja fuera (no se cancelan solas, decisión 20)
        var desdeUtc = _reloj.AUtc(fecha, inicio ?? TimeOnly.MinValue);
        var hastaUtc = fin is { } h ? _reloj.AUtc(fecha, h) : _reloj.AUtc(fecha.AddDays(1), TimeOnly.MinValue);
        var afectadas = await _db.Citas.AsNoTracking()
            .Where(x => x.Inicio < hastaUtc && x.Fin > desdeUtc && EstadosCita.Activos.Contains(x.Estado))
            .OrderBy(x => x.Inicio)
            .Select(x => new { x.Id, x.Inicio, x.DentistaId, x.Estado }).ToListAsync(ct);
        return (new CierreDto(cierre.Id, fecha, inicio, fin, cierre.Motivo),
            afectadas.Select(x => { var (fl, hl) = _reloj.ALocal(x.Inicio); return new CitaFueraDeHorario(x.Id, fl, hl, x.DentistaId, x.Estado); }).ToList());
    }

    // DELETE físico permitido para cierres (decisión 20); la bitácora lo registra
    public async Task<bool> EliminarCierreAsync(int id, CancellationToken ct = default)
    {
        var cierre = await _db.AgendaCierres.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cierre is null) return false;
        _db.AgendaCierres.Remove(cierre);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
