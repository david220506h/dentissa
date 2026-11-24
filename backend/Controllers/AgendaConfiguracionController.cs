using backend.Constants;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    // Configuración del horario y cierres (diseño de la agenda, 9.4; commit 17). Lectura: recepción. Escritura: admin.
    [ApiController]
    [Route("api/agenda")]
    [Authorize(Roles = Roles.Panel)]
    public class AgendaConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionAgendaService _config;
        private readonly IRelojClinica _reloj;

        public AgendaConfiguracionController(IConfiguracionAgendaService config, IRelojClinica reloj)
        {
            _config = config;
            _reloj = reloj;
        }

        [HttpGet("configuracion")]
        public Task<IActionResult> Obtener() => this.EjecutarAsync(async () => Ok(await _config.ObtenerAsync()));

        // Las citas futuras que queden fuera del horario nuevo se permiten y se devuelven (decisión 20)
        [HttpPut("configuracion")]
        [Authorize(Roles = Roles.Admin)]
        public Task<IActionResult> Guardar([FromBody] ConfiguracionAgendaDto dto) =>
            this.EjecutarAsync(async () => Ok(new { mensaje = "Configuración guardada", citasFueraDeHorario = await _config.GuardarAsync(dto) }));

        [HttpGet("fuera-de-horario")]
        public Task<IActionResult> FueraDeHorario() => this.EjecutarAsync(async () => Ok(await _config.CitasFueraDeHorarioAsync()));

        [HttpGet("cierres")]
        public async Task<IActionResult> Cierres([FromQuery] DateOnly? desde) =>
            Ok(await _config.CierresAsync(desde ?? _reloj.HoyLocal()));

        public sealed record CrearCierreDto(DateOnly Fecha, TimeOnly? HoraInicio, TimeOnly? HoraFin, string? Motivo);

        [HttpPost("cierres")]
        [Authorize(Roles = Roles.Admin)]
        public Task<IActionResult> CrearCierre([FromBody] CrearCierreDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var (cierre, afectadas) = await _config.CrearCierreAsync(dto.Fecha, dto.HoraInicio, dto.HoraFin, dto.Motivo);
                return StatusCode(StatusCodes.Status201Created, new { cierre, citasAfectadas = afectadas });
            });

        [HttpDelete("cierres/{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> EliminarCierre(int id) =>
            await _config.EliminarCierreAsync(id) ? NoContent() : NotFound(new { mensaje = "Cierre no encontrado" });
    }
}
