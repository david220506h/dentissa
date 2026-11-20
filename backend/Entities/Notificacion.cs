namespace backend.Entities;

// Destinatario: un usuario interno o un paciente (exactamente uno)
public class Notificacion
{
    public long Id { get; set; }

    public int? UsuarioId { get; set; }

    public int? PacienteId { get; set; }

    public int? CitaId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Canal { get; set; } = null!;

    public string? Asunto { get; set; }

    public string Mensaje { get; set; } = null!;

    public bool Leida { get; set; }

    public DateTime? LeidaEn { get; set; }

    public bool Enviada { get; set; }

    public DateTime? EnviadaEn { get; set; }

    public string? ErrorEnvio { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Usuario? Usuario { get; set; }

    public virtual Paciente? Paciente { get; set; }

    public virtual Cita? Cita { get; set; }
}
