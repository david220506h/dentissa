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

    // Solo la clase o el código del error, nunca el mensaje completo
    public string? ErrorEnvio { get; set; }

    // Bandeja de salida (solo canal "correo"; diseño de la agenda, 7.3). La fila no guarda el cuerpo renderizado
    // ni tokens: solo la plantilla y sus referencias (en Mensaje, como JSON sin datos personales).
    public string? Estado { get; set; }

    public string? Plantilla { get; set; }

    public int Intentos { get; set; }

    public DateTime? ProximoIntentoEn { get; set; }

    // Arrendamiento del trabajador que la reclamó (10 minutos)
    public DateTime? ProcesandoDesde { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Usuario? Usuario { get; set; }

    public virtual Paciente? Paciente { get; set; }

    public virtual Cita? Cita { get; set; }
}

public static class CanalesNotificacion
{
    public const string Interna = "interna";
    public const string Correo  = "correo";
}

public static class EstadosNotificacion
{
    public const string Pendiente  = "pendiente";
    public const string Procesando = "procesando";
    public const string Enviada    = "enviada";
    public const string Fallida    = "fallida";
    public const string Descartada = "descartada";

    public static readonly string[] Todos = { Pendiente, Procesando, Enviada, Fallida, Descartada };
}

public static class TiposNotificacion
{
    public const string SolicitudNueva     = "solicitud_nueva";
    public const string Confirmacion       = "confirmacion";
    public const string Recordatorio       = "recordatorio";
    public const string Cambio             = "cambio";
    public const string Cancelacion        = "cancelacion";
    public const string SolicitudCambio    = "solicitud_cambio";
    public const string AvisoConfiguracion = "aviso_configuracion";
    public const string Vencimiento        = "vencimiento";
    public const string Reasignacion       = "reasignacion";
    public const string EnvioFallido       = "envio_fallido";
    public const string TopePendientes     = "tope_pendientes";
    public const string IntentoRegistro    = "intento_registro";
    public const string Configuracion      = "configuracion";

    public static readonly string[] Todos =
    {
        SolicitudNueva, Confirmacion, Recordatorio, Cambio, Cancelacion, SolicitudCambio, AvisoConfiguracion,
        Vencimiento, Reasignacion, EnvioFallido, TopePendientes, IntentoRegistro, Configuracion
    };
}

// Plantillas de correo (diseño, 7.2)
public static class PlantillasCorreo
{
    public const string SolicitudRecibida       = "solicitud_recibida";
    public const string Confirmacion            = "confirmacion";
    public const string Recordatorio            = "recordatorio";
    public const string Cambio                  = "cambio";
    public const string Cancelacion             = "cancelacion";
    public const string SolicitudCambioRecibida = "solicitud_cambio_recibida";
    public const string Vencimiento             = "vencimiento";
    public const string TopePendientes          = "tope_pendientes";
    public const string IntentoRegistro         = "intento_registro";

    public static readonly string[] Todas =
    {
        SolicitudRecibida, Confirmacion, Recordatorio, Cambio, Cancelacion, SolicitudCambioRecibida,
        Vencimiento, TopePendientes, IntentoRegistro
    };
}
