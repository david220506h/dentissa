using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

/// <summary>
/// Lee de la base el horario de atención (configuracion_agenda, horario_dias, horario_descansos y
/// agenda_cierres) y lo entrega como objetos puros. Es el único que lee la base para la agenda (diseño, 2.4).
/// </summary>
public interface ICalendarioAtencion
{
    Task<HorarioAtencion> ObtenerHorarioAsync(CancellationToken ct = default);
    Task<ParametrosBloqueo> ObtenerParametrosAsync(CancellationToken ct = default);
    Task<RelojAtencion> ObtenerRelojAsync(CancellationToken ct = default);
}

public class CalendarioAtencion : ICalendarioAtencion
{
    private readonly DentissaDbContext _db;
    private readonly IRelojClinica _reloj;

    public CalendarioAtencion(DentissaDbContext db, IRelojClinica reloj)
    {
        _db = db;
        _reloj = reloj;
    }

    private async Task<ConfiguracionAgenda> ConfiguracionAsync(CancellationToken ct) =>
        await _db.ConfiguracionAgenda.AsNoTracking()
            .Include(c => c.Dias)
            .Include(c => c.Descansos)
            .FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, ct)
        ?? throw new AgendaNoConfiguradaException();

    public async Task<HorarioAtencion> ObtenerHorarioAsync(CancellationToken ct = default)
    {
        var config = await ConfiguracionAsync(ct);
        // Los cierres son pocos: se cargan los de hoy en adelante y los recientes (cubren cálculos hacia atrás)
        var desde = _reloj.HoyLocal().AddDays(-RelojAtencion.TopeDias);
        var cierres = await _db.AgendaCierres.AsNoTracking().Where(c => c.Fecha >= desde).ToListAsync(ct);

        return new HorarioAtencion(
            config.Dias
                .Where(d => d is { Laborable: true, HoraApertura: not null, HoraCierre: not null })
                .Select(d => new HorarioAtencion.Dia(d.DiaSemana, d.HoraApertura!.Value, d.HoraCierre!.Value)),
            config.Descansos.Select(d => new HorarioAtencion.Descanso(d.DiaSemana, d.HoraInicio, d.HoraFin)),
            cierres.Select(c => new HorarioAtencion.Cierre(c.Fecha, c.HoraInicio, c.HoraFin)),
            _reloj.Zona);
    }

    public async Task<ParametrosBloqueo> ObtenerParametrosAsync(CancellationToken ct = default)
    {
        var c = await ConfiguracionAsync(ct);
        return new ParametrosBloqueo(
            c.AnticipacionEnLineaAtencionMinutos,
            c.AnticipacionRecepcionMinutos,
            c.LiberacionAntesCitaAtencionMinutos,
            c.VidaMaximaSolicitudAtencionMinutos,
            c.HorizonteReservaDias);
    }

    public async Task<RelojAtencion> ObtenerRelojAsync(CancellationToken ct = default) => new(await ObtenerHorarioAsync(ct));
}
