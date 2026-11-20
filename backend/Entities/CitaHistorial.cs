namespace backend.Entities;

// Solo inserción: un trigger impide UPDATE, DELETE y TRUNCATE
public class CitaHistorial
{
    public long Id { get; set; }

    public int CitaId { get; set; }

    public string Accion { get; set; } = null!;

    // Nulo si actuó el paciente desde el enlace del correo
    public int? UsuarioId { get; set; }

    public string? Canal { get; set; }

    public string? Detalle { get; set; }

    // jsonb con los valores antes y después
    public string? Datos { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }
}

public static class AccionesCitaHistorial
{
    public const string Creacion            = "creacion";
    public const string Modificacion        = "modificacion";
    public const string Reagenda            = "reagenda";
    public const string CambioEstado        = "cambio_estado";
    public const string Confirmacion        = "confirmacion";
    public const string Cancelacion         = "cancelacion";
    public const string SolicitudCambio     = "solicitud_cambio";
    public const string ResolucionSolicitud = "resolucion_solicitud";

    public static readonly string[] Todas =
    {
        Creacion, Modificacion, Reagenda, CambioEstado, Confirmacion,
        Cancelacion, SolicitudCambio, ResolucionSolicitud
    };
}
