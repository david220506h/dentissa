using backend.Constants;
using backend.DTOs;
using backend.Entities;
using backend.Repositories;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtService _jwtService;
        private readonly IPacientesService _pacientes;
        private readonly IConfiguration _config;

        public AuthService(IAuthRepository authRepository, IJwtService jwtService, IPacientesService pacientes, IConfiguration config)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _pacientes = pacientes;
            _config = config;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var usuario = await _authRepository.GetByEmailAsync(loginDto.Email);

            // Verificar que existe, que el password coincide con el hash y que la cuenta
            // está activa. Las eliminadas no llegan aquí (filtro global). Mismo mensaje en
            // todos los casos para no revelar si la cuenta existe o está desactivada.
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash) || !usuario.Activo)
                throw new UnauthorizedAccessException("Credenciales incorrectas");

            usuario.UltimoAccesoEn = DateTime.UtcNow;
            return await GenerateAuthResponse(usuario);
        }

        public async Task<UsuarioResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            if (await _authRepository.EmailExistsAsync(registerDto.Email))
                throw new InvalidOperationException("El correo ya está registrado");

            var nuevoUsuario = new Usuario
            {
                Nombre = registerDto.Nombre,
                Apellido = registerDto.Apellido,
                Correo = registerDto.Email,
                Telefono = registerDto.Telefono,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password), // Hash aquí
                Rol = Roles.Paciente // El registro público nunca acepta un rol del cliente
            };
            // Cuenta y ficha de paciente se guardan juntas (un solo SaveChanges)
            nuevoUsuario.Paciente = _pacientes.NuevaFichaParaCuenta(nuevoUsuario, OrigenesCita.Portal);

            var usuarioCreado = await _authRepository.CreateAsync(nuevoUsuario);
            return MapToResponseDto(usuarioCreado);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var usuario = await _authRepository.GetByRefreshTokenAsync(refreshToken);

            if (usuario == null || !usuario.Activo || usuario.RefreshTokenExpiraEn < DateTime.UtcNow)
                throw new UnauthorizedAccessException("RefreshToken inválido o expirado");

            return await GenerateAuthResponse(usuario);
        }

        public async Task RevokeTokenAsync(int usuarioId)
        {
            var usuario = await _authRepository.GetByIdAsync(usuarioId);
            if (usuario == null) return;

            usuario.RefreshToken = null;
            usuario.RefreshTokenExpiraEn = null;
            await _authRepository.UpdateAsync(usuario);
        }

        // Genera AccessToken + RefreshToken y los guarda
        private async Task<AuthResponseDto> GenerateAuthResponse(Usuario usuario)
        {
            var accessToken = _jwtService.GenerateAccessToken(usuario);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]!);
            var expirationMinutes = int.Parse(_config["JwtSettings:ExpirationMinutes"]!);

            // Guardar RefreshToken en DB
            usuario.RefreshToken = refreshToken;
            usuario.RefreshTokenExpiraEn = DateTime.UtcNow.AddDays(refreshDays);
            await _authRepository.UpdateAsync(usuario);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Usuario = MapToResponseDto(usuario)
            };
        }

        private static UsuarioResponseDto MapToResponseDto(Usuario usuario) => new()
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Correo,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol,
            FechaRegistro = usuario.CreadoEn
        };
    }
}