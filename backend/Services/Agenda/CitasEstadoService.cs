using backend.Entities;
using backend.Repositories;
using backend.Services.Correo;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Agenda;

/// <summary>Datos de una reagenda (diseño de la agenda, 4.5).</summary>
public sealed record SolicitudReagenda(
    DateOnly Fecha,
    TimeOnly Hora,
    int? DentistaId,
    string? Motivo,
    string EstadoResultante = EstadosCita.Confirmada,
    uint? Version = null);

/// <summary>
/// Transiciones de estado de una cita (diseño, sección 4): valida con <see cref="TransicionesCita"/>, escribe el
/// historial genérico en el mismo guardado y aplica la concurrencia optimista (xmin). Los motivos libres solo
/// quedan en la fila de la cita.
/// </summary>
public interface ICitasEstadoService
{
    Task<Cita> ConfirmarAsync(int id, string? canal, uint? version = null, CancellationToken ct = default);
    Task<Cita> CancelarAsync(int id, string? motivo, string? canal, uint? version = null, CancellationToken ct = default);

    /// <summary><paramref name="dentistaUsuarioId"/>: si actúa un dentista, solo sus citas (si no, 404).</summary>
    Task<Cita> CompletarAsync(int id, int? dentistaUsuarioId, uint? version = null, CancellationToken ct = default);
    Task<Cita> NoAsistioAsync(int id, int? dentistaUsuarioId, uint? version = null, CancellationToken ct = default);
    Task<Cita> CorregirEstadoAsync(int id, string? estado, string? motivo, uint? version = null, CancellationToken ct = default);
    Task<Cita> CambiarCostosAsync(int id, decimal costoExtra, string? motivoExtra, uint? version = null, CancellationToken ct = default);
    Task<Cita> ReagendarAsync(int id, SolicitudReagenda s, CancellationToken ct = default);

    /// <summary>Reasignación en el mismo horario a otro dentista elegible y libre (3.4).</summary>
    Task<Cita> ReasignarAsync(int id, int dentistaId, string? motivo, uint? version = null, CancellationToken ct = default);

    /// <summary>Estado actual leído de la base, sin seguimiento.</summary>
    Task<string> EstadoAsync(int id, CancellationToken ct = default);
}

public class CitasEstadoService : ICitasEstadoService
{
    public static readonly string[] CanalesRecepcion = { CanalesCitaHistorial.Llamada, CanalesCitaHistorial.Panel };

    private readonly DentissaDbContext _db;
    private readonly ICitaHistorialService _historial;
    private readonly IActorActual _actor;
    private readonly IReservasService _reservas;
    private readonly TimeProvider _tiempo;
    private readonly AvisoBandeja _aviso;

    public CitasEstadoService(DentissaDbContext db, ICitaHistorialService historial, IActorActual actor, IReservasService reservas, TimeProvider tiempo, AvisoBandeja aviso)
    {
        _aviso = aviso;
        _db = db;
        _historial = historial;
        _actor = actor;
        _reservas = reservas;
        _tiempo = tiempo;
    }

    private DateTime Ahora => _tiempo.GetUtcNow().UtcDateTime;

    private static async Task<Cita> CargarAsync(DentissaDbContext db, int id, uint? version, CancellationToken ct, int? dentistaUsuarioId = null)
    {
        // Una copia ya seguida en este contexto puede estar vieja (p. ej., tras una reagenda en otro contexto)
        foreach (var vieja in db.ChangeTracker.Entries<Cita>().Where(x => x.Entity.Id == id && x.State == EntityState.Unchanged).ToList())
            vieja.State = EntityState.Detached;
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new CitaNoEncontradaException();
        // Un dentista solo ve sus citas: las ajenas responden igual que las inexistentes
        if (dentistaUsuarioId is { } usuario && !await db.Dentistas.AnyAsync(d => d.Id == cita.DentistaId && d.UsuarioId == usuario, ct))
            throw new CitaNoEncontradaException();
        if (version is { } v)
        {
            if (v != cita.Version) throw new CitaModificadaException();
            db.Entry(cita).Property(c => c.Version).OriginalValue = v;
        }
        return cita;
    }

    private async Task<Cita> GuardarAsync(Cita cita, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            _aviso.Avisar();
            return cita;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new CitaModificadaException(ex);
        }
    }

    private static string ValidarCanal(string? canal) =>
        canal is null ? CanalesCitaHistorial.Llamada
        : CanalesRecepcion.Contains(canal) ? canal
        : throw new ArgumentException("Canal inválido: usa \"llamada\" o \"panel\".");

    private static string ValidarMotivo(string? motivo, string mensaje) =>
        string.IsNullOrWhiteSpace(motivo) ? throw new ArgumentException(mensaje) : motivo.Trim();

    private static async Task ValidarElegibleAsync(IServiceProvider sp, DentissaDbContext db, int servicioId, int dentista, CancellationToken ct)
    {
        var servicio = await db.Servicios.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == servicioId, ct);
        if (!(await sp.GetRequiredService<IDisponibilidadService>().DentistasElegiblesAsync(servicio, ct)).Contains(dentista))
            throw new DentistaNoElegibleException();
    }

    public Task<Cita> ReasignarAsync(int id, int dentistaId, string? motivo, uint? version = null, CancellationToken ct = default)
    {
        var motivoLimpio = ValidarMotivo(motivo, "El motivo de la reasignación es obligatorio");
        return _reservas.EjecutarReservaAsync(async (sp, db, token) =>
        {
            var cita = await CargarAsync(db, id, version, token);
            TransicionesCita.ValidarReasignacion(cita, dentistaId);
            await ValidarElegibleAsync(sp, db, cita.ServicioId, dentistaId, token);

            var horario = await sp.GetRequiredService<ICalendarioAtencion>().ObtenerHorarioAsync(token);
            await ReservasService.TomarCandadosAsync(db, [horario.FechaLocal(cita.Inicio)], token);
            var ahora = sp.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
            if (await ReservasService.OcupadoAsync(db, dentistaId, cita.Inicio, cita.Fin, ahora, token, excluir: cita.Id))
                throw new HorarioOcupadoException();
            var historial = sp.GetRequiredService<ICitaHistorialService>();
            if (await ReservasService.HayVencidasAsync(db, [dentistaId], cita.Inicio, cita.Fin, ahora, token, excluir: cita.Id))
                await ReservasService.LiberarVencidasAsync(db, historial, sp.GetRequiredService<ContextoAuditoria>(), [dentistaId], cita.Inicio, cita.Fin, ahora,
                    new { citaId = cita.Id, usuarioId = sp.GetRequiredService<IActorActual>().UsuarioId }, token, excluir: cita.Id);

            var antes = cita.DentistaId;
            cita.DentistaId = dentistaId;
            cita.MotivoCambio = motivoLimpio;
            historial.Registrar(cita, AccionesCitaHistorial.Reasignacion, "Dentista reasignado por recepción",
                new { antes = new { dentistaId = antes }, despues = new { dentistaId } });
            try
            {
                await db.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new CitaModificadaException(ex);
            }
            return cita;
        }, ct);
    }

    public async Task<string> EstadoAsync(int id, CancellationToken ct = default) =>
        await _db.Citas.AsNoTracking().Where(c => c.Id == id).Select(c => c.Estado).SingleAsync(ct);

    public async Task<Cita> ConfirmarAsync(int id, string? canal, uint? version = null, CancellationToken ct = default)
    {
        canal = ValidarCanal(canal);
        var cita = await CargarAsync(_db, id, version, ct);
        if (!TransicionesCita.ValidarConfirmar(cita)) return cita;

        // Recepción puede confirmar una vencida aún no cancelada: sigue ocupando su horario y deja de vencer (4.2)
        cita.Estado            = EstadosCita.Confirmada;
        cita.ConfirmadaEn      = Ahora;
        cita.ConfirmadaPor     = _actor.UsuarioId;
        cita.ConfirmacionCanal = canal;
        cita.BloqueoExpiraEn   = null;
        _historial.Registrar(cita, AccionesCitaHistorial.Confirmacion,
            canal == CanalesCitaHistorial.Llamada ? "Confirmada por recepción (llamada)" : "Confirmada por recepción", canal: canal);
        BandejaCorreo.Encolar(_db, cita, PlantillasCorreo.Confirmacion, TiposNotificacion.Confirmacion, Ahora);
        return await GuardarAsync(cita, ct);
    }

    public async Task<Cita> CancelarAsync(int id, string? motivo, string? canal, uint? version = null, CancellationToken ct = default)
    {
        motivo = ValidarMotivo(motivo, "El motivo de cancelación es obligatorio");
        canal = canal is null ? CanalesCitaHistorial.Panel : ValidarCanal(canal);
        var cita = await CargarAsync(_db, id, version, ct);
        TransicionesCita.ValidarCancelar(cita);

        cita.Estado            = EstadosCita.Cancelada;
        cita.MotivoCancelacion = motivo;
        cita.CanceladaEn       = Ahora;
        _historial.Registrar(cita, AccionesCitaHistorial.Cancelacion, "Cancelada por recepción", canal: canal);
        // Sin el motivo (7.2)
        BandejaCorreo.Encolar(_db, cita, PlantillasCorreo.Cancelacion, TiposNotificacion.Cancelacion, Ahora);
        return await GuardarAsync(cita, ct);
    }

    public async Task<Cita> CompletarAsync(int id, int? dentistaUsuarioId, uint? version = null, CancellationToken ct = default)
    {
        var cita = await CargarAsync(_db, id, version, ct, dentistaUsuarioId);
        TransicionesCita.ValidarCompletar(cita, Ahora);
        var antes = cita.Estado;
        cita.Estado = EstadosCita.Completada;
        _historial.Registrar(cita, AccionesCitaHistorial.CambioEstado, $"{antes} → {cita.Estado}");
        return await GuardarAsync(cita, ct);
    }

    public async Task<Cita> NoAsistioAsync(int id, int? dentistaUsuarioId, uint? version = null, CancellationToken ct = default)
    {
        var cita = await CargarAsync(_db, id, version, ct, dentistaUsuarioId);
        TransicionesCita.ValidarNoAsistio(cita, Ahora);
        var antes = cita.Estado;
        cita.Estado = EstadosCita.NoAsistio;
        _historial.Registrar(cita, AccionesCitaHistorial.CambioEstado, $"{antes} → {cita.Estado}");
        return await GuardarAsync(cita, ct);
    }

    public async Task<Cita> CorregirEstadoAsync(int id, string? estado, string? motivo, uint? version = null, CancellationToken ct = default)
    {
        motivo = ValidarMotivo(motivo, "El motivo de la corrección es obligatorio");
        var hacia = estado?.Trim().ToLowerInvariant() ?? "";
        var cita = await CargarAsync(_db, id, version, ct);
        TransicionesCita.ValidarCorreccion(cita, hacia, Ahora);
        var antes = cita.Estado;
        cita.Estado = hacia;
        cita.MotivoCambio = motivo;
        _historial.Registrar(cita, AccionesCitaHistorial.CorreccionEstado, $"Corrección de administración: {antes} → {hacia}");
        return await GuardarAsync(cita, ct);
    }

    public async Task<Cita> CambiarCostosAsync(int id, decimal costoExtra, string? motivoExtra, uint? version = null, CancellationToken ct = default)
    {
        if (costoExtra < 0) throw new ArgumentException("El costo extra no puede ser negativo");
        var cita = await CargarAsync(_db, id, version, ct);
        TransicionesCita.ValidarCostos(cita);
        var antes = new { cita.CostoExtra, cita.CostoTotal };
        cita.CostoExtra  = costoExtra;
        cita.MotivoExtra = string.IsNullOrWhiteSpace(motivoExtra) ? null : motivoExtra.Trim();
        cita.CostoTotal  = CostosCita.Total(cita.CostoBase, cita.DescuentoAplicado, cita.CostoExtra);
        if (antes.CostoExtra == cita.CostoExtra && antes.CostoTotal == cita.CostoTotal && !_db.Entry(cita).Property(c => c.MotivoExtra).IsModified)
            return cita;
        _historial.Registrar(cita, AccionesCitaHistorial.Modificacion, "Cambio de costos",
            new { antes, despues = new { cita.CostoExtra, cita.CostoTotal } });
        return await GuardarAsync(cita, ct);
    }

    /// <summary>
    /// Reagenda (4.5): candado de la fecha vieja y la nueva (en orden), reglas de recepción, liberación de vencidas en
    /// el horario nuevo y ocupación del dentista. Conserva el precio original; queda confirmada por defecto.
    /// </summary>
    public Task<Cita> ReagendarAsync(int id, SolicitudReagenda s, CancellationToken ct = default) =>
        _reservas.EjecutarReservaAsync(async (sp, db, token) =>
        {
            var historial = sp.GetRequiredService<ICitaHistorialService>();
            var calendario = sp.GetRequiredService<ICalendarioAtencion>();
            var tiempo = sp.GetRequiredService<TimeProvider>();
            var cita = await CargarAsync(db, id, s.Version, token);
            TransicionesCita.ValidarReagenda(cita, s.EstadoResultante);

            var dentista = s.DentistaId ?? cita.DentistaId;
            if (dentista != cita.DentistaId)
                await ValidarElegibleAsync(sp, db, cita.ServicioId, dentista, token);
            var duracion = await db.Servicios.IgnoreQueryFilters().Where(x => x.Id == cita.ServicioId).Select(x => x.DuracionSlots).SingleAsync(token);
            var config = await db.ConfiguracionAgenda.AsNoTracking().FirstOrDefaultAsync(c => c.Id == ConfiguracionAgenda.IdUnico, token)
                ?? throw new AgendaNoConfiguradaException();
            var horario = await calendario.ObtenerHorarioAsync(token);
            var opcion = horario.InicioPosibles(s.Fecha, config.DuracionSlotMinutos, duracion).Where(o => o.Hora == s.Hora).ToList();
            if (opcion.Count == 0) throw new HoraFueraDeHorarioException();
            var (_, inicio, fin) = opcion[0];

            await ReservasService.TomarCandadosAsync(db, [horario.FechaLocal(cita.Inicio), s.Fecha], token);
            var ahora = tiempo.GetUtcNow().UtcDateTime;
            var regla = ReglaBloqueo.CalcularBloqueo(ahora, inicio, CanalReserva.Recepcion, new RelojAtencion(horario), await calendario.ObtenerParametrosAsync(token));
            if (!regla.Aceptada)
                throw new ReservaRechazadaException(regla.Motivo!.Value, ReservasService.MensajeRechazo(regla.Motivo.Value, CanalReserva.Recepcion));
            if (await ReservasService.OcupadoAsync(db, dentista, inicio, fin, ahora, token, excluir: cita.Id))
                throw new HorarioOcupadoException();
            if (await ReservasService.HayVencidasAsync(db, [dentista], inicio, fin, ahora, token, excluir: cita.Id))
                await ReservasService.LiberarVencidasAsync(db, historial, sp.GetRequiredService<ContextoAuditoria>(), [dentista], inicio, fin, ahora,
                    new { citaId = cita.Id, usuarioId = sp.GetRequiredService<IActorActual>().UsuarioId }, token, excluir: cita.Id);

            var antes = new { cita.Inicio, cita.DentistaId, cita.Estado };
            cita.Inicio          = inicio;
            cita.Fin             = fin;
            cita.DentistaId      = dentista;
            cita.BloqueoExpiraEn = null;
            cita.MotivoCambio    = string.IsNullOrWhiteSpace(s.Motivo) ? cita.MotivoCambio : s.Motivo.Trim();
            if (s.EstadoResultante == EstadosCita.Confirmada && cita.Estado != EstadosCita.Confirmada)
            {
                cita.ConfirmadaEn      = ahora;
                cita.ConfirmadaPor     = sp.GetRequiredService<IActorActual>().UsuarioId;
                cita.ConfirmacionCanal = CanalesCitaHistorial.Panel;
            }
            cita.Estado = s.EstadoResultante;
            historial.Registrar(cita, AccionesCitaHistorial.Reagenda, "Reagendada por recepción",
                new { antes, despues = new { cita.Inicio, cita.DentistaId, cita.Estado } });
            if (antes.Inicio != cita.Inicio)
                BandejaCorreo.Encolar(db, cita, PlantillasCorreo.Cambio, TiposNotificacion.Cambio, ahora, new ReferenciasCorreo(antes.Inicio));
            try
            {
                await db.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new CitaModificadaException(ex);
            }
            return cita;
        }, ct);
}
