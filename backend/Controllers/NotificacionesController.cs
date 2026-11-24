using System.Security.Claims;
using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    // Bandeja interna del personal (diseño de la agenda, 9.4; commit 18): cada cuenta ve solo sus avisos
    // (recepción: correos no enviados; administración además: avisos de configuración).
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize(Roles = Roles.Panel)]
    public class NotificacionesController : ControllerBase
    {
        public const int Maximo = 100;

        private readonly DentissaDbContext _db;
        private readonly TimeProvider _tiempo;

        public NotificacionesController(DentissaDbContext db, TimeProvider tiempo)
        {
            _db = db;
            _tiempo = tiempo;
        }

        private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── GET: api/notificaciones/internas?noLeidas=true ───
        [HttpGet("internas")]
        public async Task<IActionResult> Internas([FromQuery] bool noLeidas)
        {
            var yo = UsuarioId;
            var q = _db.Notificaciones.AsNoTracking()
                .Where(n => n.UsuarioId == yo && n.Canal == CanalesNotificacion.Interna);
            if (noLeidas) q = q.Where(n => !n.Leida);
            var filas = await q.OrderByDescending(n => n.CreadoEn).ThenByDescending(n => n.Id).Take(Maximo)
                .Select(n => new { n.Id, n.Tipo, n.Asunto, n.Mensaje, n.CitaId, n.Leida, n.CreadoEn })
                .ToListAsync();
            var sinLeer = await _db.Notificaciones.CountAsync(n => n.UsuarioId == yo && n.Canal == CanalesNotificacion.Interna && !n.Leida);
            return Ok(new { sinLeer, notificaciones = filas });
        }

        // ── PATCH: api/notificaciones/5/leida ────────────────
        [HttpPatch("{id}/leida")]
        public async Task<IActionResult> MarcarLeida(long id)
        {
            var yo = UsuarioId;
            var n = await _db.Notificaciones.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == yo && x.Canal == CanalesNotificacion.Interna);
            if (n is null) return NotFound(new { mensaje = "Aviso no encontrado" });
            if (!n.Leida)
            {
                n.Leida = true;
                n.LeidaEn = _tiempo.GetUtcNow().UtcDateTime;
                await _db.SaveChangesAsync();
            }
            return Ok(new { mensaje = "Aviso leído" });
        }
    }
}
