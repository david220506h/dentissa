using System.Security.Claims;
using backend.Constants;
using backend.DTOs;
using backend.Entities;
using backend.Repositories;
using backend.Services;
using backend.Services.Agenda;
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
        private readonly ICitaHistorialService _historial;
        private readonly IDisponibilidadService _disponibilidad;
        private readonly IReservasService _reservas;

        public CitasController(ICitasRepository repo, IAgendaService agenda, IRelojClinica reloj, ICitaHistorialService historial, IDisponibilidadService disponibilidad, IReservasService reservas)
        {
            _reservas = reservas;
            _repo = repo;
            _agenda = agenda;
            _reloj = reloj;
            _historial = historial;
            _disponibilidad = disponibilidad;
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

            // Candado por fecha, liberación de vencidas, regla de recepción y reintentos (diseño, sección 6)
            try
            {
                var cita = await _reservas.ReservarAsync(new SolicitudReserva(
                    dto.PacienteId, dto.DentistaId, dto.ServicioId, dto.Fecha, dto.Hora,
                    CanalReserva.Recepcion, OrigenesCita.Recepcion, "Cita creada desde el panel",
                    dto.Motivo, dto.CostoExtra, dto.MotivoExtra));
                return CreatedAtAction(nameof(GetById), new { id = cita.Id }, new { cita.Id, mensaje = "Cita creada correctamente" });
            }
            catch (AgendaNoConfiguradaException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (ServicioNoDisponibleException)
            {
                return BadRequest(new { mensaje = "Servicio no encontrado o inactivo" });
            }
            catch (HoraFueraDeHorarioException ex)
            {
                return BadRequest(new { mensaje = ex.Message, codigo = "HORA_FUERA_DE_HORARIO" });
            }
            catch (ReservaRechazadaException ex)
            {
                return BadRequest(new { mensaje = ex.Message, codigo = CodigoRechazo(ex.Motivo) });
            }
            catch (HorarioOcupadoException ex)
            {
                return Conflict(new { mensaje = ex.Message, codigo = "HORARIO_OCUPADO" });
            }
        }

        private static string CodigoRechazo(MotivoRechazo motivo) => motivo switch
        {
            MotivoRechazo.EnElPasado => "HORARIO_PASADO",
            MotivoRechazo.FueraDeHorizonte => "FUERA_DE_HORIZONTE",
            _ => "ANTICIPACION_INSUFICIENTE",
        };

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
                var antes = new { cita.DentistaId, cita.Inicio, cita.Fin, cita.Estado, cita.CostoExtra, cita.MotivoExtra };
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
                cita.CostoTotal  = CostosCita.Total(cita.CostoBase, cita.DescuentoAplicado, cita.CostoExtra);

                var despues = new { cita.DentistaId, cita.Inicio, cita.Fin, cita.Estado, cita.CostoExtra, cita.MotivoExtra };
                var datos = new { antes, despues };
                if (cambioHorario)
                    _historial.Registrar(cita, AccionesCitaHistorial.Reagenda, "Cambio de fecha, hora o dentista", datos);
                if (antes.Estado != cita.Estado)
                    _historial.Registrar(cita, AccionesCitaHistorial.CambioEstado, $"{antes.Estado} → {cita.Estado}", datos);
                if (!cambioHorario && antes.Estado == cita.Estado && !Equals(antes, despues))
                    _historial.Registrar(cita, AccionesCitaHistorial.Modificacion, "Cambio de costos", datos);

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

        // ── PATCH: api/Citas/5/cancelar ──────────────────────
        // Una cita no se borra nunca: se cancela con motivo (RN-02)
        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarCitaDto dto)
        {
            var motivo = dto.Motivo?.Trim();
            if (string.IsNullOrEmpty(motivo))
                return BadRequest(new { mensaje = "El motivo de cancelación es obligatorio" });

            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            if (!EstadosCita.Activos.Contains(cita.Estado))
                return BadRequest(new { mensaje = "Solo se puede cancelar una cita pendiente o confirmada" });

            cita.Estado            = EstadosCita.Cancelada;
            cita.MotivoCancelacion = motivo;
            cita.CanceladaEn       = DateTime.UtcNow;
            _historial.Registrar(cita, AccionesCitaHistorial.Cancelacion, motivo);
            await _repo.UpdateAsync(cita);
            return Ok(new { mensaje = "Cita cancelada", estado = cita.Estado });
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
                _historial.Registrar(cita, AccionesCitaHistorial.Confirmacion, "Confirmada por recepción (llamada)");
                await _repo.UpdateAsync(cita);
            }
            return Ok(new { mensaje = "Cita confirmada", estado = cita.Estado });
        }

        private int? UsuarioActualId()
            => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private CitaRespuestaDto ToRespuestaDto(Cita c) => CitaMapper.ARespuesta(c, _reloj);
    }
}
