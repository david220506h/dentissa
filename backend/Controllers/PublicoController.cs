using backend.Repositories;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    // Endpoints públicos sin credencial (diseño de la agenda, 9.1). Límite por IP en RateLimiting:Publico.
    // Nunca exponen dentistas ni datos de pacientes.
    [ApiController]
    [Route("api/publico")]
    [AllowAnonymous]
    public class PublicoController : ControllerBase
    {
        private readonly IDisponibilidadService _disponibilidad;
        private readonly ISolicitudesPublicasService _solicitudes;
        private readonly backend.Entities.DentissaDbContext _db;
        private readonly int _maxDias;

        public PublicoController(IDisponibilidadService disponibilidad, ISolicitudesPublicasService solicitudes,
            backend.Entities.DentissaDbContext db, IConfiguration config)
        {
            _disponibilidad = disponibilidad;
            _solicitudes = solicitudes;
            _db = db;
            _maxDias = config.GetValue<int?>("Agenda:DisponibilidadPublicaMaxDias") ?? 14;
        }

        // ── GET: api/publico/servicios ── Servicios activos para el sitio (nombre, precio y horarios que ocupa)
        [HttpGet("servicios")]
        public async Task<IActionResult> Servicios(CancellationToken ct) =>
            Ok(await _db.Servicios.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre, s.Precio, duracionSlots = s.DuracionSlots }).ToListAsync(ct));

        // ── POST: api/publico/solicitudes ── Solicitud de cita sin cuenta: aparta el horario con el bloqueo y el
        // vencimiento de la agenda (diseño, sección 2) hasta que recepción la confirme o la rechace. Límite propio por IP.
        [HttpPost("solicitudes")]
        public async Task<IActionResult> Solicitar([FromBody] SolicitudPublicaDto dto, CancellationToken ct)
        {
            try
            {
                var creada = await _solicitudes.CrearAsync(dto, ct);
                // Campo trampa: la misma respuesta que un éxito, sin datos
                return StatusCode(StatusCodes.Status201Created, creada is null
                    ? new { mensaje = "Recibimos tu solicitud." }
                    : new { mensaje = "Recibimos tu solicitud.", creada.Fecha, creada.Hora, creada.Servicio });
            }
            catch (HorarioOcupadoException)
            {
                return Conflict(new { mensaje = "Ese horario se acaba de ocupar. Elige otro, por favor.", codigo = "HORARIO_OCUPADO" });
            }
            catch (Exception ex) when (RespuestasAgenda.DesdeExcepcion(this, ex) is { } respuesta)
            {
                return respuesta;
            }
        }

        // ── GET: api/publico/disponibilidad?servicioId=3&desde=2026-12-01&hasta=2026-12-07
        // Solo horas, sin dentistas (1.2). Servicio inexistente o inactivo: el mismo 400 genérico.
        [HttpGet("disponibilidad")]
        public async Task<IActionResult> Disponibilidad([FromQuery] int servicioId, [FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken ct)
        {
            if (servicioId <= 0 || !DateOnly.TryParseExact(desde, "yyyy-MM-dd", out var inicio) || !DateOnly.TryParseExact(hasta, "yyyy-MM-dd", out var fin))
                return BadRequest(new { mensaje = "Revisa el servicio y las fechas (YYYY-MM-DD).", codigo = "PARAMETROS_INVALIDOS" });

            try
            {
                var horarios = await _disponibilidad.CalcularAsync(inicio, fin, servicioId, CanalReserva.EnLinea, maxDias: _maxDias, ct: ct);
                var porFecha = horarios.ToLookup(h => h.Fecha);
                var dias = new List<object>();
                for (var f = inicio; f <= fin; f = f.AddDays(1))
                    dias.Add(new { fecha = f.ToString("yyyy-MM-dd"), horas = porFecha[f].Select(h => h.Hora.ToString("HH:mm")).ToList() });
                return Ok(dias);
            }
            catch (ServicioNoDisponibleException)
            {
                return BadRequest(new { mensaje = "Revisa el servicio y las fechas (YYYY-MM-DD).", codigo = "PARAMETROS_INVALIDOS" });
            }
            catch (RangoInvalidoException ex)
            {
                return BadRequest(new { mensaje = ex.Message, codigo = "RANGO_INVALIDO" });
            }
            catch (AgendaNoConfiguradaException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { mensaje = "La agenda no está disponible por ahora. Llámanos para agendar.", codigo = "AGENDA_NO_CONFIGURADA" });
            }
        }
    }
}
