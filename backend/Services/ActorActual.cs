using System.Security.Claims;

namespace backend.Services
{
    // Quién hace la petición actual (desde el token) y desde qué IP.
    // Fuera de una petición HTTP (arranque, tareas) todo es nulo.
    public interface IActorActual
    {
        int? UsuarioId { get; }
        string? Correo { get; }
        string? Ip { get; }
    }

    public class ActorActual : IActorActual
    {
        private readonly IHttpContextAccessor _http;

        public ActorActual(IHttpContextAccessor http) => _http = http;

        private ClaimsPrincipal? Usuario => _http.HttpContext?.User;

        public int? UsuarioId
            => int.TryParse(Usuario?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public string? Correo => Usuario?.FindFirstValue(ClaimTypes.Email);

        public string? Ip => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
