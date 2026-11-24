using System.Security.Claims;
using backend.Constants;
using backend.Services.Pacientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    // Pacientes para el panel (Fase 2b; docs/pacientes/propuesta-api-pacientes.md, aprobada solo para la demo con
    // datos ficticios). Lectura: administración, asistente y dentista (solo sus asignados, sin teléfono).
    // Escritura: administración y asistente. Sin expediente clínico.
    [ApiController]
    [Route("api/pacientes")]
    [Authorize(Roles = Roles.Panel + "," + Roles.Dentista)]
    public class PacientesController : ControllerBase
    {
        private readonly IPacientesPanelService _pacientes;

        public PacientesController(IPacientesPanelService pacientes) => _pacientes = pacientes;

        private Consultante Quien => new(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.IsInRole(Roles.Dentista));

        // ── GET: api/pacientes?buscar=nunez&pagina=1&tamano=25
        [HttpGet]
        public Task<IActionResult> Listar([FromQuery] string? buscar, [FromQuery] int pagina = 1, [FromQuery] int tamano = PacientesPanelService.TamanoPorDefecto) =>
            this.EjecutarAsync(async () => Ok(await _pacientes.ListarAsync(Quien, buscar, pagina, tamano)));

        // ── GET: api/pacientes/5 (un dentista sin cita con el paciente recibe 404, no 403)
        [HttpGet("{id}")]
        public async Task<IActionResult> Detalle(int id) =>
            await _pacientes.DetalleAsync(Quien, id) is { } p ? Ok(p) : NotFound(new { mensaje = "Paciente no encontrado" });

        [HttpPost]
        [Authorize(Roles = Roles.Panel)]
        public Task<IActionResult> Crear([FromBody] PacienteEdicionDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var p = await _pacientes.CrearAsync(Quien, dto);
                return CreatedAtAction(nameof(Detalle), new { id = p.Id }, p);
            });

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Panel)]
        public Task<IActionResult> Editar(int id, [FromBody] PacienteEdicionDto dto) =>
            this.EjecutarAsync(async () =>
                await _pacientes.EditarAsync(Quien, id, dto) is { } p ? Ok(p) : NotFound(new { mensaje = "Paciente no encontrado" }));

        public sealed record AlergiasDto(string? Alergias);

        [HttpPatch("{id}/alergias")]
        [Authorize(Roles = Roles.Panel)]
        public Task<IActionResult> Alergias(int id, [FromBody] AlergiasDto dto) =>
            this.EjecutarAsync(async () =>
                await _pacientes.CambiarAlergiasAsync(Quien, id, dto.Alergias) is { } p ? Ok(p) : NotFound(new { mensaje = "Paciente no encontrado" }));

        // Baja lógica (nada se borra físicamente)
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Panel)]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            try
            {
                return await _pacientes.DarDeBajaAsync(id) ? NoContent() : NotFound(new { mensaje = "Paciente no encontrado" });
            }
            catch (PacienteConCitasException ex)
            {
                return Conflict(new { mensaje = ex.Message, codigo = "PACIENTE_CON_CITAS" });
            }
        }
    }
}
