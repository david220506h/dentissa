using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly int _maxDias;

        public PublicoController(IDisponibilidadService disponibilidad, IConfiguration config)
        {
            _disponibilidad = disponibilidad;
            _maxDias = config.GetValue<int?>("Agenda:DisponibilidadPublicaMaxDias") ?? 14;
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
