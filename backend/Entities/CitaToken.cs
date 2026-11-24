namespace backend.Entities;

// Enlace de un solo uso enviado por correo (diseño de la agenda, 5.1 y 5.2). Solo se guarda el hash SHA-256 del
// token: el token en claro existe únicamente en el correo y en la memoria de quien lo envía. Sin borrado físico.
public class CitaToken
{
    public long Id { get; set; }

    public int CitaId { get; set; }

    public string Proposito { get; set; } = null!;

    // 64 caracteres hexadecimales en minúsculas
    [DatoSensible]
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiraEn { get; set; }

    public DateTime? UsadoEn { get; set; }

    // Un reenvío invalida los anteriores del mismo propósito
    public DateTime? InvalidadoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cita Cita { get; set; } = null!;
}

public static class PropositosToken
{
    public const string Confirmar = "confirmar";
    public const string Gestionar = "gestionar";

    public static readonly string[] Todos = { Confirmar, Gestionar };
}
