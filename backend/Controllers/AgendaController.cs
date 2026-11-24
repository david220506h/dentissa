using backend.Constants;
using backend.Entities;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    // Agenda del panel (diseño de la agenda, 9.4). Recepción = asistente y administración.
    [ApiController]
    [Route("api/agenda")]
    [Authorize(Roles = Roles.Panel)]
    public class AgendaController : ControllerBase
    {
        private readonly IDisponibilidadService _disponibilidad;
        private readonly DentissaDbContext _db;

        public AgendaController(IDisponibilidadService disponibilidad, DentissaDbContext db)
        {
            _disponibilidad = disponibilidad;
            _db = db;
        }

        // ── GET: api/agenda/disponibilidad?fecha=2026-12-01&servicioId=3&dentistaId=2
        // Con las reglas de recepción (anticipación en minutos de reloj) y los dentistas libres de cada horario.
        [HttpGet("disponibilidad")]
        public async Task<IActionResult> Disponibilidad([FromQuery] string? fecha, [FromQuery] int servicioId, [FromQuery] int? dentistaId, CancellationToken ct)
        {
            if (servicioId <= 0 || !DateOnly.TryParseExact(fecha, "yyyy-MM-dd", out var dia))
                return BadRequest(new { mensaje = "Revisa el servicio y la fecha (YYYY-MM-DD).", codigo = "PARAMETROS_INVALIDOS" });

            try
            {
                var horarios = await _disponibilidad.CalcularAsync(dia, dia, servicioId, CanalReserva.Recepcion, dentistaId, ct: ct);
                var ids = horarios.SelectMany(h => h.DentistasLibres).Distinct().ToList();
                var nombres = await _db.Dentistas.AsNoTracking()
                    .Where(d => ids.Contains(d.Id))
                    .Select(d => new { d.Id, d.Titulo, d.Usuario.Nombre, d.Usuario.Apellido })
                    .ToDictionaryAsync(d => d.Id, ct);

                return Ok(horarios.Select(h => new
                {
                    hora = h.Hora.ToString("HH:mm"),
                    inicio = h.InicioUtc,
                    fin = h.FinUtc,
                    dentistasLibres = h.DentistasLibres.Select(id => new
                    {
                        id,
                        nombre = string.Join(' ', new[] { nombres[id].Titulo, nombres[id].Nombre, nombres[id].Apellido }.Where(p => !string.IsNullOrWhiteSpace(p)))
                    })
                }));
            }
            catch (ServicioNoDisponibleException)
            {
                return BadRequest(new { mensaje = "El servicio no está disponible.", codigo = "SERVICIO_NO_DISPONIBLE" });
            }
            catch (AgendaNoConfiguradaException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = ex.Message, codigo = "AGENDA_NO_CONFIGURADA" });
            }
        }
    }
}
