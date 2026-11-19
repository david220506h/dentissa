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
    [Authorize(Roles = Roles.Admin)]
    public class UsuariosController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;

        public UsuariosController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        // ── GET: api/Usuarios ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _authRepository.GetAllAsync();
            var result = usuarios.Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Email,
                u.Telefono,
                u.Rol,
                fechaRegistro = u.FechaRegistro.HasValue
                    ? u.FechaRegistro.Value.ToString("dd/MM/yyyy")
                    : ""
            });
            return Ok(result);
        }

        // ── POST: api/Usuarios ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearUsuarioDto dto)
        {
            if (await _authRepository.EmailExistsAsync(dto.Email))
                return BadRequest(new { mensaje = "El correo ya está registrado" });

            if (string.IsNullOrWhiteSpace(dto.Rol) || !Roles.Todos.Contains(dto.Rol.ToLower()))
                return BadRequest(new { mensaje = "Rol inválido" });

            // Redundante con [Authorize] del controlador, pero protege si se abre a otros roles
            if (dto.Rol.ToLower() == Roles.Admin && !User.IsInRole(Roles.Admin))
                return Forbid();

            var usuario = new Usuario
            {
                Nombre       = dto.Nombre,
                Apellido     = dto.Apellido,
                Email        = dto.Email,
                Telefono     = dto.Telefono,
                Password     = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol          = dto.Rol.ToLower(),
                FechaRegistro = DateTime.Now
            };

            var creado = await _authRepository.CreateAsync(usuario);
            return Ok(new
            {
                creado.Id,
                creado.Nombre,
                creado.Apellido,
                creado.Email,
                creado.Telefono,
                creado.Rol,
                fechaRegistro = creado.FechaRegistro?.ToString("dd/MM/yyyy"),
                mensaje = "Usuario creado correctamente"
            });
        }

        // ── DELETE: api/Usuarios/5 ────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuario = await _authRepository.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            await _authRepository.DeleteAsync(usuario);
            return Ok(new { mensaje = "Usuario eliminado correctamente" });
        }
    }
}