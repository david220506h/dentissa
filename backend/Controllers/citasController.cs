using System.Security.Claims;
using backend.Constants;
using backend.DTOs;
using backend.Entities;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public CitasController(ICitasRepository repo, IAgendaService agenda, IRelojClinica reloj)
        {
            _repo = repo;
            _agenda = agenda;
            _reloj = reloj;
        }

        // ── GET: api/Citas ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var citas = await _repo.GetAllAsync();
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
        [HttpGet("horarios-disponibles")]
        public async Task<IActionResult> GetHorariosDisponibles([FromQuery] string fecha, [FromQuery] int? dentistaId)
        {
            if (!DateOnly.TryParse(fecha, out var fechaParsed))
                return BadRequest(new { mensaje = "Formato inválido. Use YYYY-MM-DD" });

            try
            {
                return Ok(await _agenda.HorariosDisponiblesAsync(fechaParsed, dentistaId));
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
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CitaDto dto)
        {
            var servicio = await _repo.GetServicioActivoAsync(dto.ServicioId);
            if (servicio == null)
                return BadRequest(new { mensaje = "Servicio no encontrado o inactivo" });

            if (await _repo.GetPacienteAsync(dto.PacienteId) == null)
                return BadRequest(new { mensaje = "Paciente no encontrado" });

            if (await _repo.GetDentistaActivoAsync(dto.DentistaId) == null)
                return BadRequest(new { mensaje = "Dentista no encontrado o inactivo" });

            try
            {
                if (!(await _agenda.HorariosDelDiaAsync(dto.Fecha)).Contains(dto.Hora))
                    return BadRequest(new { mensaje = "La hora no corresponde a un horario de la clínica" });

                var (inicio, fin) = await _agenda.CalcularIntervaloAsync(dto.Fecha, dto.Hora, servicio.DuracionSlots);

                var oferta = await _repo.GetOfertaVigenteAsync(dto.ServicioId, dto.Fecha);
                var costoBase = servicio.Precio;
                var descuento = oferta?.Descuento ?? 0;
                var extra = dto.CostoExtra ?? 0;

                var cita = new Cita
                {
                    PacienteId        = dto.PacienteId,
                    DentistaId        = dto.DentistaId,
                    ServicioId        = dto.ServicioId,
                    OfertaId          = oferta?.Id,
                    Inicio            = inicio,
                    Fin               = fin,
                    Estado            = EstadosCita.Pendiente,
                    Origen            = OrigenesCita.Recepcion,
                    CreadoPor         = UsuarioActualId(),
                    Motivo            = dto.Motivo,
                    CostoBase         = costoBase,
                    DescuentoAplicado = descuento,
                    CostoExtra        = extra,
                    MotivoExtra       = dto.MotivoExtra,
                    CostoTotal        = CalcularTotal(costoBase, descuento, extra)
                };

                await _repo.CreateAsync(cita);
                return CreatedAtAction(nameof(GetById), new { id = cita.Id }, new { cita.Id, mensaje = "Cita creada correctamente" });
            }
            catch (AgendaNoConfiguradaException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (HorarioOcupadoException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        // ── PUT: api/Citas/5 ──────────────────────────────────
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

            try
            {
                var (fechaActual, horaActual) = _reloj.ALocal(cita.Inicio);
                var cambioHorario = fechaActual != dto.Fecha || horaActual != dto.Hora || cita.DentistaId != dto.DentistaId;

                if (cambioHorario)
                {
                    if (cita.DentistaId != dto.DentistaId && await _repo.GetDentistaActivoAsync(dto.DentistaId) == null)
                        return BadRequest(new { mensaje = "Dentista no encontrado o inactivo" });

                    if ((fechaActual != dto.Fecha || horaActual != dto.Hora) &&
                        !(await _agenda.HorariosDelDiaAsync(dto.Fecha)).Contains(dto.Hora))
                        return BadRequest(new { mensaje = "La hora no corresponde a un horario de la clínica" });

                    var (inicio, fin) = await _agenda.CalcularIntervaloAsync(dto.Fecha, dto.Hora, cita.Servicio.DuracionSlots);
                    cita.DentistaId = dto.DentistaId;
                    cita.Inicio     = inicio;
                    cita.Fin        = fin;
                }

                cita.Estado      = estado;
                cita.CostoExtra  = dto.CostoExtra ?? 0;
                cita.MotivoExtra = dto.MotivoExtra;
                cita.CostoTotal  = CalcularTotal(cita.CostoBase, cita.DescuentoAplicado, cita.CostoExtra);

                await _repo.UpdateAsync(cita);
                return Ok(new { mensaje = "Cita actualizada correctamente" });
            }
            catch (AgendaNoConfiguradaException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (HorarioOcupadoException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        // ── DELETE: api/Citas/5 ───────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            await _repo.DeleteAsync(cita);
            return Ok(new { mensaje = "Cita eliminada correctamente" });
        }

        // ── PATCH: api/Citas/5/confirmar ─────────────────────
        // Confirmación desde el panel: recepción llamó al paciente
        [HttpPatch("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(int id)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            if (cita.Estado != EstadosCita.Pendiente && cita.Estado != EstadosCita.Confirmada)
                return BadRequest(new { mensaje = "Solo se puede confirmar una cita pendiente" });

            if (cita.Estado == EstadosCita.Pendiente)
            {
                cita.Estado            = EstadosCita.Confirmada;
                cita.ConfirmadaEn      = DateTime.UtcNow;
                cita.ConfirmadaPor     = UsuarioActualId();
                cita.ConfirmacionCanal = "llamada";
                await _repo.UpdateAsync(cita);
            }
            return Ok(new { mensaje = "Cita confirmada", estado = cita.Estado });
        }

        private int? UsuarioActualId()
            => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private static decimal CalcularTotal(decimal costoBase, decimal descuento, decimal extra)
            => Math.Round(costoBase - (costoBase * descuento / 100) + extra, 2, MidpointRounding.AwayFromZero);

        private CitaRespuestaDto ToRespuestaDto(Cita c)
        {
            var (fecha, hora) = _reloj.ALocal(c.Inicio);
            return new CitaRespuestaDto
            {
                Id                = c.Id,
                Paciente          = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                Servicio          = c.Servicio.Nombre,
                Dentista          = c.Dentista.Usuario.Nombre + " " + c.Dentista.Usuario.Apellido,
                Fecha             = fecha.ToString("dd/MM/yyyy"),
                Hora              = hora.ToString("HH:mm"),
                Inicio            = DateTime.SpecifyKind(c.Inicio, DateTimeKind.Utc),
                Fin               = DateTime.SpecifyKind(c.Fin, DateTimeKind.Utc),
                Estado            = c.Estado,
                Origen            = c.Origen,
                Motivo            = c.Motivo,
                MotivoCancelacion = c.MotivoCancelacion,
                SolicitudCambio   = c.SolicitudCambioTipo == null ? null : new SolicitudCambioDto
                {
                    Tipo   = c.SolicitudCambioTipo,
                    Motivo = c.SolicitudCambioMotivo,
                    En     = DateTime.SpecifyKind(c.SolicitudCambioEn!.Value, DateTimeKind.Utc)
                },
                CostoBase         = c.CostoBase,
                DescuentoAplicado = c.DescuentoAplicado,
                CostoExtra        = c.CostoExtra,
                MotivoExtra       = c.MotivoExtra,
                CostoTotal        = c.CostoTotal,
                PacienteId        = c.PacienteId,
                DentistaId        = c.DentistaId,
                ServicioId        = c.ServicioId,
                Oferta            = c.Oferta?.Descuento
            };
        }
    }
}
