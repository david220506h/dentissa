namespace backend.Entities;

public class Usuario : IConMarcasDeTiempo, IEliminable
{
    public int Id { get; set; }

    public string Correo { get; set; } = null!;

    [DatoSensible]
    public string PasswordHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string? Telefono { get; set; }

    public bool Activo { get; set; } = true;

    [DatoSensible, ControlDeSesion]
    public string? RefreshToken { get; set; }

    [ControlDeSesion]
    public DateTime? RefreshTokenExpiraEn { get; set; }

    [ControlDeSesion]
    public DateTime? UltimoAccesoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual Paciente? Paciente { get; set; }

    public virtual Dentista? Dentista { get; set; }
}
