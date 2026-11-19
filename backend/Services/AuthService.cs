using backend.DTOs;
using backend.Entities;
using backend.Repositories;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;

        public AuthService(IAuthRepository authRepository, IJwtService jwtService, IConfiguration config)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _config = config;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var usuario = await _authRepository.GetByEmailAsync(loginDto.Email);

            // Verificar que existe y que el password coincide con el hash
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.Password))
                throw new UnauthorizedAccessException("Credenciales incorrectas");

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
                Email = registerDto.Email,
                Telefono = registerDto.Telefono,
                Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password), // Hash aquí
                Rol = "cliente",
                FechaRegistro = DateTime.Now
            };

            var usuarioCreado = await _authRepository.CreateAsync(nuevoUsuario);
            return MapToResponseDto(usuarioCreado);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var usuario = await _authRepository.GetByRefreshTokenAsync(refreshToken);

            if (usuario == null || usuario.RefreshTokenExpiration < DateTime.UtcNow)
                throw new UnauthorizedAccessException("RefreshToken inválido o expirado");

            return await GenerateAuthResponse(usuario);
        }

        public async Task RevokeTokenAsync(int usuarioId)
        {
            var usuario = await _authRepository.GetByIdAsync(usuarioId);
            if (usuario == null) return;

            usuario.RefreshToken = null;
            usuario.RefreshTokenExpiration = null;
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
            usuario.RefreshTokenExpiration = DateTime.UtcNow.AddDays(refreshDays);
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
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol,
            FechaRegistro = usuario.FechaRegistro
        };
    }
}