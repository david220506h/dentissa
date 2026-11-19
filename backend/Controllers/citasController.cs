using backend.Constants;
using backend.DTOs;
using backend.Entities;
using backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Panel)]
    public class CitasController : ControllerBase
    {
        private readonly ICitasRepository _repo;

        public CitasController(ICitasRepository repo)
        {
            _repo = repo;
        }

        // ── GET: api/Citas ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var citas = await _repo.GetAllAsync();

            var result = citas.Select(c => new
            {
                c.Id,
                paciente        = c.Cliente.Usuario.Nombre + " " + c.Cliente.Usuario.Apellido,
                servicio        = c.Servicio.Nombre,
                trabajador      = c.Trabajador.Usuario.Nombre + " " + c.Trabajador.Usuario.Apellido,
                fecha           = c.Fecha.ToString("dd/MM/yyyy"),
                hora            = c.Hora.ToString("HH:mm"),
                estado          = c.Estado,
                c.CostoBase,
                c.DescuentoAplicado,
                c.CostoExtra,
                c.MotivoExtra,
                c.CostoTotal,
                c.AgendadoPor,
                c.ClienteId,
                c.TrabajadorId,
                c.ServicioId,
                oferta          = c.Oferta != null ? c.Oferta.Descuento : (decimal?)null
            });

            return Ok(result);
        }

        // ── GET: api/Citas/5 ──────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });
            return Ok(cita);
        }

        // ── GET: api/Citas/horarios-disponibles?fecha=2024-11-27
        [HttpGet("horarios-disponibles")]
        public async Task<IActionResult> GetHorariosDisponibles([FromQuery] string fecha)
        {
            if (!DateOnly.TryParse(fecha, out var fechaParsed))
                return BadRequest(new { mensaje = "Formato inválido. Use YYYY-MM-DD" });

            var horarios = await _repo.GetHorariosDisponiblesAsync(fechaParsed);
            return Ok(horarios);
        }

        // ── GET: api/Citas/clientes ───────────────────────────
        [HttpGet("clientes")]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _repo.GetClientesAsync();
            var result = clientes.Select(c => new
            {
                c.Id,
                nombre   = c.Usuario.Nombre,
                apellido = c.Usuario.Apellido,
            });
            return Ok(result);
        }

        // ── GET: api/Citas/trabajadores ───────────────────────
        [HttpGet("trabajadores")]
        public async Task<IActionResult> GetTrabajadores()
        {
            var trabajadores = await _repo.GetTrabajadoresAsync();
            var result = trabajadores.Select(t => new
            {
                t.Id,
                nombre       = t.Usuario.Nombre,
                apellido     = t.Usuario.Apellido,
                t.Especialidad,
                t.Titulo
            });
            return Ok(result);
        }

        // ── GET: api/Citas/servicios ──────────────────────────
        [HttpGet("servicios")]
        public async Task<IActionResult> GetServicios()
        {
            var servicios = await _repo.GetServiciosAsync();
            var result = servicios.Select(s => new
            {
                s.Id,
                s.Nombre,
                s.Precio,
                s.Duracion
            });
            return Ok(result);
        }

        // ── POST: api/Citas ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CitaDto dto)
        {
            var servicio = await _repo.GetServicioAsync(dto.ServicioId);
            if (servicio == null)
                return BadRequest(new { mensaje = "Servicio no encontrado" });

            var ocupado = await _repo.TrabajadorOcupadoAsync(dto.TrabajadorId, dto.Fecha, dto.Hora);
            if (ocupado)
                return BadRequest(new { mensaje = "El trabajador ya tiene una cita en ese horario" });

            var oferta    = await _repo.GetOfertaActivaAsync(dto.ServicioId);
            decimal base_ = servicio.Precio;
            decimal desc  = oferta?.Descuento ?? 0;
            decimal extra = dto.CostoExtra ?? 0;
            decimal total = base_ - (base_ * desc / 100) + extra;

            var cita = new Cita
            {
                ClienteId         = dto.ClienteId,
                TrabajadorId      = dto.TrabajadorId,
                ServicioId        = dto.ServicioId,
                OfertaId          = oferta?.Id,
                Fecha             = dto.Fecha,
                Hora              = dto.Hora,
                Estado            = "pendiente",
                CostoBase         = base_,
                DescuentoAplicado = desc,
                CostoExtra        = extra,
                MotivoExtra       = dto.MotivoExtra,
                CostoTotal        = total,
                AgendadoPor       = dto.AgendadoPor
            };

            await _repo.CreateAsync(cita);
            return CreatedAtAction(nameof(GetById), new { id = cita.Id }, new { cita.Id, mensaje = "Cita creada correctamente" });
        }

        // ── PUT: api/Citas/5 ──────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Editar(int id, [FromBody] CitaEditDto dto)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            bool cambioHorario =
                cita.Fecha        != dto.Fecha ||
                cita.Hora         != dto.Hora  ||
                cita.TrabajadorId != dto.TrabajadorId;

            if (cambioHorario)
            {
                var ocupado = await _repo.TrabajadorOcupadoAsync(dto.TrabajadorId, dto.Fecha, dto.Hora, id);
                if (ocupado)
                    return BadRequest(new { mensaje = "El trabajador ya tiene una cita en ese horario" });
            }

            cita.TrabajadorId = dto.TrabajadorId;
            cita.Fecha        = dto.Fecha;
            cita.Hora         = dto.Hora;
            cita.Estado       = dto.Estado;
            cita.CostoExtra   = dto.CostoExtra ?? 0;
            cita.MotivoExtra  = dto.MotivoExtra;
            cita.CostoTotal   = cita.CostoBase - (cita.CostoBase * (cita.DescuentoAplicado ?? 0) / 100) + (cita.CostoExtra ?? 0);

            await _repo.UpdateAsync(cita);
            return Ok(new { mensaje = "Cita actualizada correctamente" });
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
        [HttpPatch("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(int id)
        {
            var cita = await _repo.GetByIdAsync(id);
            if (cita == null) return NotFound(new { mensaje = "Cita no encontrada" });

            cita.Estado = "confirmada";
            await _repo.UpdateAsync(cita);
            return Ok(new { mensaje = "Cita confirmada", estado = cita.Estado });
        }
    }
}