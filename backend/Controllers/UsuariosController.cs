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
    [Authorize(Roles = Roles.Admin)]
    public class UsuariosController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPacientesService _pacientes;

        public UsuariosController(IAuthRepository authRepository, IPacientesService pacientes)
        {
            _authRepository = authRepository;
            _pacientes = pacientes;
        }

        // ── GET: api/Usuarios ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _authRepository.GetAllAsync();
            return Ok(usuarios.Select(u => ToListaDto(u, new UsuarioListaDto())));
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
                Correo       = dto.Email,
                Telefono     = dto.Telefono,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol          = dto.Rol.ToLower()
            };

            // La ficha se guarda junto con la cuenta (un solo SaveChanges)
            if (usuario.Rol == Roles.Dentista)
                usuario.Dentista = new Dentista
                {
                    Usuario           = usuario,
                    CedulaProfesional = dto.CedulaProfesional,
                    Especialidad      = dto.Especialidad
                };
            else if (usuario.Rol == Roles.Paciente)
                usuario.Paciente = _pacientes.NuevaFichaParaCuenta(usuario, OrigenesCita.Recepcion);

            var creado = await _authRepository.CreateAsync(usuario);
            var respuesta = ToListaDto(creado, new UsuarioCreadoDto());
            respuesta.Mensaje = "Usuario creado correctamente";
            return Ok(respuesta);
        }

        // ── PATCH: api/Usuarios/5/desactivar ─────────────────
        // Una cuenta desactivada no inicia sesión ni renueva su token
        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuario = await _authRepository.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actualId) && actualId == id)
                return BadRequest(new { mensaje = "No puedes desactivar tu propia cuenta" });

            usuario.Activo = false;
            usuario.RefreshToken = null;
            usuario.RefreshTokenExpiraEn = null;
            try
            {
                await _authRepository.UpdateAsync(usuario);
            }
            catch (UltimoAdministradorException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            return Ok(new { mensaje = "Usuario desactivado", activo = false });
        }

        // ── PATCH: api/Usuarios/5/activar ────────────────────
        [HttpPatch("{id}/activar")]
        public async Task<IActionResult> Activar(int id)
        {
            var usuario = await _authRepository.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            usuario.Activo = true;
            await _authRepository.UpdateAsync(usuario);
            return Ok(new { mensaje = "Usuario activado", activo = true });
        }

        private static T ToListaDto<T>(Usuario u, T dto) where T : UsuarioListaDto
        {
            dto.Id            = u.Id;
            dto.Nombre        = u.Nombre;
            dto.Apellido      = u.Apellido;
            dto.Email         = u.Correo;
            dto.Telefono      = u.Telefono;
            dto.Rol           = u.Rol;
            dto.Activo        = u.Activo;
            dto.FechaRegistro = u.CreadoEn.ToString("dd/MM/yyyy");
            dto.UltimoAccesoEn = u.UltimoAccesoEn;
            return dto;
        }
    }
}