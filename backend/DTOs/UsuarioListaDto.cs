namespace backend.DTOs
{
    // Respuesta de api/Usuarios: nunca incluye Password ni RefreshToken
    public class UsuarioListaDto
    {
        public int     Id            { get; set; }
        public string  Nombre        { get; set; } = string.Empty;
        public string  Apellido      { get; set; } = string.Empty;
        public string  Email         { get; set; } = string.Empty;
        public string? Telefono      { get; set; }
        public string  Rol           { get; set; } = string.Empty;
        public bool    Activo        { get; set; }
        public string  FechaRegistro { get; set; } = string.Empty;
    }

    public class UsuarioCreadoDto : UsuarioListaDto
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}
