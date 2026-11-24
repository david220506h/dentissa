using backend.Constants;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    // Bandeja de solicitudes del sitio para recepción (administración y asistente). Confirmar y rechazar usan
    // PATCH /api/citas/{id}/confirmar y /cancelar (con motivo), que ya registran historial y correo.
    [ApiController]
    [Route("api/solicitudes")]
    [Authorize(Roles = Roles.Panel)]
    public class SolicitudesController : ControllerBase
    {
        private readonly ISolicitudesPublicasService _solicitudes;

        public SolicitudesController(ISolicitudesPublicasService solicitudes) => _solicitudes = solicitudes;

        // Pendientes del sitio o el portal que aún apartan su horario (no vencidas y por venir)
        [HttpGet]
        public async Task<IActionResult> Bandeja(CancellationToken ct) => Ok(await _solicitudes.BandejaAsync(ct));
    }
}
