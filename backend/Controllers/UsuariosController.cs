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

        private static T ToListaDto<T>(Usuario u, T dto) where T : UsuarioListaDto
        {
            dto.Id            = u.Id;
            dto.Nombre        = u.Nombre;
            dto.Apellido      = u.Apellido;
            dto.Email         = u.Correo;
            dto.Telefono      = u.Telefono;
            dto.Rol           = u.Rol;
            dto.FechaRegistro = u.CreadoEn.ToString("dd/MM/yyyy");
            return dto;
        }
    }
}