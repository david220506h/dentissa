using System.Security.Claims;
using backend.Constants;
using backend.DTOs;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace backend.Controllers
{
    // Completar y no asistió (diseño de la agenda, 9.4): recepción en cualquier cita; el dentista solo en las
    // suyas (una ajena responde 404). Las reglas viven en TransicionesCita.
    [ApiController]
    [Route("api/citas")]
    [Authorize(Roles = Roles.Panel + "," + Roles.Dentista)]
    public class CitasAtencionController : ControllerBase
    {
        private readonly ICitasEstadoService _estados;

        public CitasAtencionController(ICitasEstadoService estados) => _estados = estados;

        // Si actúa un dentista, su cuenta; recepción no tiene restricción por dentista
        private int? DentistaUsuarioId() =>
            User.IsInRole(Roles.Dentista) && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        // ── PATCH: api/citas/5/completar ─────────────────────
        [HttpPatch("{id}/completar")]
        public Task<IActionResult> Completar(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccionCitaDto? dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.CompletarAsync(id, DentistaUsuarioId(), dto?.Version);
                return Ok(new { mensaje = "Cita completada", estado = cita.Estado, version = cita.Version });
            });

        // ── PATCH: api/citas/5/no-asistio ────────────────────
        [HttpPatch("{id}/no-asistio")]
        public Task<IActionResult> NoAsistio(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccionCitaDto? dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.NoAsistioAsync(id, DentistaUsuarioId(), dto?.Version);
                return Ok(new { mensaje = "Inasistencia registrada", estado = cita.Estado, version = cita.Version });
            });
    }
}
