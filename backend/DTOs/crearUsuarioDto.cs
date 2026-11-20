using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class CrearUsuarioDto
    {
        public string  Nombre   { get; set; } = null!;
        public string  Apellido { get; set; } = null!;
        public string  Email    { get; set; } = null!;
        public string? Telefono { get; set; }
        public string  Password { get; set; } = null!;
        public string  Rol      { get; set; } = null!;

        // Solo con rol dentista (opcionales)
        [MaxLength(20)]  public string? CedulaProfesional { get; set; }
        [MaxLength(100)] public string? Especialidad      { get; set; }
    }
}
