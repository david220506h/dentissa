namespace backend.Entities;

// Bitácora inmodificable: un trigger impide UPDATE, DELETE y TRUNCATE
public class Auditoria
{
    public long Id { get; set; }

    public DateTime OcurridoEn { get; set; }

    // Nulo en eventos anónimos (p. ej., login fallido)
    public int? UsuarioId { get; set; }

    // Copia al momento del evento
    public string? UsuarioCorreo { get; set; }

    public string Accion { get; set; } = null!;

    public string Entidad { get; set; } = null!;

    public string? EntidadId { get; set; }

    // jsonb
    public string? Detalle { get; set; }

    public string? Ip { get; set; }

    // "sistema" en acciones automáticas (sin usuario); nulo cuando actúa la cuenta de la petición o un anónimo
    public string? Actor { get; set; }

    // jsonb: qué desencadenó una acción del sistema (p. ej., {"citaId": 12, "usuarioId": 3})
    public string? ProvocadoPor { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
