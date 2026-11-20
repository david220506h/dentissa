using System.Security.Claims;
using backend.Constants;
using backend.DTOs;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    // Agenda del dentista: solo lectura y solo sus propias citas. El dentista sale del
    // token, nunca de la petición. Va en su propio controlador porque CitasController
    // exige el rol de recepción en todas sus acciones.
    [ApiController]
    [Route("api/citas/mi-agenda")]
    [Authorize(Roles = Roles.Dentista)]
    public class MiAgendaController : ControllerBase
    {
        private const int DiasPorDefecto = 30;
        private const int DiasMaximos = 92;

        private readonly ICitasRepository _repo;
        private readonly IRelojClinica _reloj;

        public MiAgendaController(ICitasRepository repo, IRelojClinica reloj)
        {
            _repo = repo;
            _reloj = reloj;
        }

        // ── GET: api/citas/mi-agenda?desde=2026-11-20&hasta=2026-11-27 (fechas locales, inclusivas)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
        {
            var inicio = desde ?? _reloj.HoyLocal();
            var fin = hasta ?? inicio.AddDays(DiasPorDefecto);
            if (fin < inicio)
                return BadRequest(new { mensaje = "La fecha final no puede ser anterior a la inicial" });
            if (fin.DayNumber - inicio.DayNumber > DiasMaximos)
                return BadRequest(new { mensaje = $"El rango máximo es de {DiasMaximos} días" });

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
                return Unauthorized();

            var dentista = await _repo.GetDentistaPorUsuarioAsync(usuarioId);
            if (dentista == null)
                return NotFound(new { mensaje = "La cuenta no tiene ficha de dentista" });

            var citas = await _repo.GetAgendaDentistaAsync(
                dentista.Id,
                _reloj.AUtc(inicio, TimeOnly.MinValue),
                _reloj.AUtc(fin.AddDays(1), TimeOnly.MinValue));

            return Ok(citas.Select(c => CitaMapper.ARespuesta(c, _reloj)));
        }
    }
}
