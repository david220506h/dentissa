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

    public virtual Usuario? Usuario { get; set; }
}
