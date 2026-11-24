using System.Security.Claims;
using backend.Constants;
using backend.DTOs;
using backend.Entities;
using backend.Repositories;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Panel)]
    public class CitasController : ControllerBase
    {
        // Estados que se asignan editando; la cancelación tiene su propia acción
        private static readonly string[] EstadosEditables =
            { EstadosCita.Pendiente, EstadosCita.Confirmada, EstadosCita.Completada, EstadosCita.NoAsistio };

        private readonly ICitasRepository _repo;
        private readonly IAgendaService _agenda;
        private readonly IRelojClinica _reloj;
        private readonly ICitaHistorialService _historial;
        private readonly IDisponibilidadService _disponibilidad;
        private readonly IReservasService _reservas;
        private readonly ICitasEstadoService _estados;

        public CitasController(ICitasRepository repo, IAgendaService agenda, IRelojClinica reloj, ICitaHistorialService historial, IDisponibilidadService disponibilidad, IReservasService reservas, ICitasEstadoService estados)
        {
            _reservas = reservas;
            _estados = estados;
            _repo = repo;
            _agenda = agenda;
            _reloj = reloj;
            _historial = historial;
            _disponibilidad = disponibilidad;
        }

        // ── GET: api/Citas ────────────────────────────────────
        // ?desde&hasta (fechas locales, inclusivas; máximo 92 días), ?dentistaId, ?estado: todos opcionales; sin
        // parámetros responde como siempre (plan 3a, 14.7). ?dentistaInactivo=true: citas futuras activas de
        // dentistas desactivados o eliminados (diseño, 3.5).
        public const int DiasMaximosFiltro = 92;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool dentistaInactivo, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
            [FromQuery] int? dentistaId, [FromQuery] string? estado, [FromServices] TimeProvider tiempo)
        {
            if (desde is { } d1 && hasta is { } h1)
            {
                if (h1 < d1) return BadRequest(new { mensaje = "La fecha final no puede ser anterior a la inicial" });
                if (h1.DayNumber - d1.DayNumber + 1 > DiasMaximosFiltro)
                    return BadRequest(new { mensaje = $"El rango máximo es de {DiasMaximosFiltro} días" });
            }
            var estadoFiltro = estado?.Trim().ToLowerInvariant();
            if (estadoFiltro is not null && !EstadosCita.Todos.Contains(estadoFiltro))
                return BadRequest(new { mensaje = "Estado inválido" });

            var citas = dentistaInactivo
                ? await _repo.GetConDentistaInactivoAsync(tiempo.GetUtcNow().UtcDateTime)
                : desde is null && hasta is null && dentistaId is null && estadoFiltro is null
                    ? await _repo.GetAllAsync()
                    : await _repo.GetFiltradasAsync(
                        desde is { } d ? _reloj.AUtc(d, TimeOnly.MinValue) : null,
                        hasta is { } h ? _reloj.AUtc(h.AddDays(1), TimeOnly.MinValue) : null,
                        dentistaId, estadoFiltro);
            return Ok(citas.Select(ToRespuestaDto));
        }

        // ── GET: api/Citas/5 ──────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });
            return Ok(ToRespuestaDto(cita));
        }

        // ── GET: api/Citas/horarios-disponibles?fecha=2026-11-27&dentistaId=3
        // OBSOLETO (compatibilidad, diseño 9.4): delega en el motor de disponibilidad con las reglas de
        // recepción y servicios de un horario. Usar GET api/agenda/disponibilidad. Se retira con autorización.
        [HttpGet("horarios-disponibles")]
        public async Task<IActionResult> GetHorariosDisponibles([FromQuery] string fecha, [FromQuery] int? dentistaId)
        {
            if (!DateOnly.TryParse(fecha, out var fechaParsed))
                return BadRequest(new { mensaje = "Formato inválido. Use YYYY-MM-DD" });

            try
            {
                var horarios = await _disponibilidad.CalcularSinServicioAsync(fechaParsed, dentistaId);
                return Ok(horarios.Select(h => h.Hora.ToString("HH:mm")).ToList());
            }
            catch (AgendaNoConfiguradaException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        // ── GET: api/Citas/pacientes ──────────────────────────
        [HttpGet("pacientes")]
        public async Task<IActionResult> GetPacientes()
        {
            var pacientes = await _repo.GetPacientesAsync();
            return Ok(pacientes.Select(p => new PacienteOpcionDto
            {
                Id       = p.Id,
                Nombre   = p.Nombre,
                Apellido = p.Apellido,
            }));
        }

        // ── GET: api/Citas/dentistas ──────────────────────────
        [HttpGet("dentistas")]
        public async Task<IActionResult> GetDentistas()
        {
            var dentistas = await _repo.GetDentistasActivosAsync();
            return Ok(dentistas.Select(d => new DentistaOpcionDto
            {
                Id           = d.Id,
                Nombre       = d.Usuario.Nombre,
                Apellido     = d.Usuario.Apellido,
                Especialidad = d.Especialidad,
                Titulo       = d.Titulo
            }));
        }

        // ── GET: api/Citas/servicios ──────────────────────────
        [HttpGet("servicios")]
        public async Task<IActionResult> GetServicios()
        {
            var servicios = await _repo.GetServiciosActivosAsync();
            return Ok(servicios.Select(s => new ServicioOpcionDto
            {
                Id            = s.Id,
                Nombre        = s.Nombre,
                Precio        = s.Precio,
                DuracionSlots = s.DuracionSlots
            }));
        }

        // ── POST: api/Citas ───────────────────────────────────
        // Candado por fecha, liberación de vencidas, regla de recepción y reintentos (diseño, sección 6)
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CitaDto dto)
        {
            if (await _repo.GetServicioActivoAsync(dto.ServicioId) == null)
                return BadRequest(new { mensaje = "Servicio no encontrado o inactivo" });

            if (await _repo.GetPacienteAsync(dto.PacienteId) == null)
                return BadRequest(new { mensaje = "Paciente no encontrado" });

            if (dto.DentistaId is { } dentistaId && await _repo.GetDentistaActivoAsync(dentistaId) == null)
                return BadRequest(new { mensaje = "Dentista no encontrado o inactivo" });

            return await this.EjecutarAsync(async () =>
            {
                var cita = await _reservas.ReservarAsync(new SolicitudReserva(
                    dto.PacienteId, dto.DentistaId, dto.ServicioId, dto.Fecha, dto.Hora,
                    dto.PacientePresente ? CanalReserva.PacientePresente : CanalReserva.Recepcion, OrigenesCita.Recepcion,
                    dto.PacientePresente ? "Cita creada desde el panel con el paciente presente" : "Cita creada desde el panel",
                    dto.Motivo, dto.CostoExtra, dto.MotivoExtra, dto.ConfirmarAlCrear));
                return CreatedAtAction(nameof(GetById), new { id = cita.Id },
                    new { cita.Id, mensaje = "Cita creada correctamente", estado = cita.Estado, dentistaId = cita.DentistaId, version = cita.Version });
            });
        }

        // ── GET: api/Citas/5/historial ────────────────────────
        // Textos genéricos: los motivos libres no están en el historial (4.6)
        [HttpGet("{id}/historial")]
        public async Task<IActionResult> Historial(int id, [FromServices] DentissaDbContext db)
        {
            if (!await db.Citas.AnyAsync(c => c.Id == id)) return NotFound(new { mensaje = "Cita no encontrada" });
            var filas = await db.CitaHistorial.AsNoTracking()
                .Where(h => h.CitaId == id)
                .OrderBy(h => h.CreadoEn).ThenBy(h => h.Id)
                .Select(h => new
                {
                    h.Accion, h.Canal, h.Detalle, h.CreadoEn, h.UsuarioId,
                    Usuario = h.Usuario == null ? null : h.Usuario.Nombre + " " + h.Usuario.Apellido
                })
                .ToListAsync();
            return Ok(filas);
        }

        // ── PUT: api/Citas/5 ──────────────────────────────────
        // OBSOLETO (compatibilidad, diseño 9.4): delega en la reagenda, los costos y las transiciones. Cada parte
        // se valida con TransicionesCita y se guarda por separado. Se retira con el panel nuevo, con autorización.
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] CitaEditDto dto)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            if (cita.Estado is EstadosCita.Completada or EstadosCita.Cancelada)
                return BadRequest(new { mensaje = "Una cita completada o cancelada no se puede editar" });

            var estado = dto.Estado?.Trim().ToLowerInvariant() ?? cita.Estado;
            if (estado == EstadosCita.Cancelada)
                return BadRequest(new { mensaje = "Para cancelar una cita usa la acción Cancelar" });
            if (!EstadosEditables.Contains(estado))
                return BadRequest(new { mensaje = "Estado inválido" });

            return await this.EjecutarAsync(async () =>
            {
                var (fechaActual, horaActual) = _reloj.ALocal(cita.Inicio);
                if (fechaActual != dto.Fecha || horaActual != dto.Hora || cita.DentistaId != dto.DentistaId)
                {
                    var resultante = EstadosCita.Activos.Contains(estado) ? estado : cita.Estado;
                    await _estados.ReagendarAsync(id, new SolicitudReagenda(dto.Fecha, dto.Hora, dto.DentistaId, null, resultante));
                }

                if ((dto.CostoExtra ?? 0) != cita.CostoExtra || dto.MotivoExtra != cita.MotivoExtra)
                    await _estados.CambiarCostosAsync(id, dto.CostoExtra ?? 0, dto.MotivoExtra);

                var actual = await _estados.EstadoAsync(id);
                if (estado != actual)
                {
                    _ = estado switch
                    {
                        EstadosCita.Confirmada => await _estados.ConfirmarAsync(id, CanalesCitaHistorial.Llamada),
                        EstadosCita.Completada => await _estados.CompletarAsync(id, null),
                        EstadosCita.NoAsistio  => await _estados.NoAsistioAsync(id, null),
                        _ => throw new TransicionInvalidaException("Una cita confirmada no vuelve a pendiente; reagéndala como pendiente."),
                    };
                }
                return Ok(new { mensaje = "Cita actualizada correctamente" });
            });
        }

        // ── PATCH: api/Citas/5/cancelar ──────────────────────
        // Una cita no se borra nunca: se cancela con motivo (RN-02); el motivo solo queda en la cita
        [HttpPatch("{id}/cancelar")]
        public Task<IActionResult> Cancelar(int id, [FromBody] CancelarCitaDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.CancelarAsync(id, dto.Motivo, dto.Canal, dto.Version);
                return Ok(new { mensaje = "Cita cancelada", estado = cita.Estado, version = cita.Version });
            });

        // ── PATCH: api/Citas/5/confirmar ─────────────────────
        // Confirmación desde el panel: "llamada" (por defecto) o "panel"; también una vencida aún no cancelada
        [HttpPatch("{id}/confirmar")]
        public Task<IActionResult> Confirmar(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccionCitaDto? dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.ConfirmarAsync(id, dto?.Canal, dto?.Version);
                return Ok(new { mensaje = "Cita confirmada", estado = cita.Estado, version = cita.Version });
            });

        // ── PATCH: api/Citas/5/reagendar ─────────────────────
        [HttpPatch("{id}/reagendar")]
        public Task<IActionResult> Reagendar(int id, [FromBody] ReagendarCitaDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.ReagendarAsync(id, new SolicitudReagenda(
                    dto.Fecha, dto.Hora, dto.DentistaId, dto.Motivo,
                    dto.EstadoResultante?.Trim().ToLowerInvariant() ?? EstadosCita.Confirmada, dto.Version));
                return Ok(new { mensaje = "Cita reagendada", estado = cita.Estado, version = cita.Version });
            });

        // ── PATCH: api/Citas/5/dentista ──────────────────────
        // Reasignación en el mismo horario (diseño, 3.4); sin correo al paciente (decisión 14)
        [HttpPatch("{id}/dentista")]
        public Task<IActionResult> Reasignar(int id, [FromBody] ReasignarDentistaDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.ReasignarAsync(id, dto.DentistaId, dto.Motivo, dto.Version);
                return Ok(new { mensaje = "Dentista reasignado", dentistaId = cita.DentistaId, version = cita.Version });
            });

        // ── PATCH: api/Citas/5/costos ────────────────────────
        [HttpPatch("{id}/costos")]
        public Task<IActionResult> Costos(int id, [FromBody] CostosCitaDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.CambiarCostosAsync(id, dto.CostoExtra, dto.MotivoExtra, dto.Version);
                return Ok(new { mensaje = "Costos actualizados", costoTotal = cita.CostoTotal, version = cita.Version });
            });

        // ── PATCH: api/Citas/5/corregir-estado ───────────────
        // Solo administración: completada ↔ no asistió, con motivo, hasta 7 días (decisión 7)
        [HttpPatch("{id}/corregir-estado")]
        [Authorize(Roles = Roles.Admin)]
        public Task<IActionResult> CorregirEstado(int id, [FromBody] CorregirEstadoDto dto) =>
            this.EjecutarAsync(async () =>
            {
                var cita = await _estados.CorregirEstadoAsync(id, dto.Estado, dto.Motivo, dto.Version);
                return Ok(new { mensaje = "Estado corregido", estado = cita.Estado, version = cita.Version });
            });

        private CitaRespuestaDto ToRespuestaDto(Cita c) => CitaMapper.ARespuesta(c, _reloj);
    }
}
