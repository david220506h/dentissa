namespace backend.Entities;

public class Cita : IConMarcasDeTiempo
{
    public int Id { get; set; }

    public int PacienteId { get; set; }

    public int DentistaId { get; set; }

    public int ServicioId { get; set; }

    public int? OfertaId { get; set; }

    // Instantes en UTC; fin = inicio + duracion_slots × duracion_slot_minutos
    public DateTime Inicio { get; set; }

    public DateTime Fin { get; set; }

    public string Estado { get; set; } = EstadosCita.Pendiente;

    public string Origen { get; set; } = null!;

    public string? Motivo { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? CanceladaEn { get; set; }

    public int? CreadoPor { get; set; }

    public decimal CostoBase { get; set; }

    public decimal DescuentoAplicado { get; set; }

    public decimal CostoExtra { get; set; }

    public string? MotivoExtra { get; set; }

    public decimal CostoTotal { get; set; }

    // Solo solicitudes del paciente (web, portal)
    public DateTime? BloqueoExpiraEn { get; set; }

    [DatoSensible]
    public string? ConfirmacionTokenHash { get; set; }

    public DateTime? ConfirmacionTokenExpiraEn { get; set; }

    public DateTime? ConfirmadaEn { get; set; }

    public int? ConfirmadaPor { get; set; }

    public string? ConfirmacionCanal { get; set; }

    public string? SolicitudCambioTipo { get; set; }

    public string? SolicitudCambioMotivo { get; set; }

    public DateTime? SolicitudCambioEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual Paciente Paciente { get; set; } = null!;

    public virtual Dentista Dentista { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Oferta? Oferta { get; set; }

    public virtual Usuario? CreadoPorUsuario { get; set; }

    public virtual Usuario? ConfirmadaPorUsuario { get; set; }

    public virtual ICollection<CitaHistorial> Historial { get; set; } = new List<CitaHistorial>();

    public virtual ICollection<ConsultaRegistro> ConsultaRegistros { get; set; } = new List<ConsultaRegistro>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual ICollection<ExamenClinico> ExamenesClinicos { get; set; } = new List<ExamenClinico>();
}

public static class EstadosCita
{
    public const string Pendiente  = "pendiente";
    public const string Confirmada = "confirmada";
    public const string Completada = "completada";
    public const string Cancelada  = "cancelada";
    public const string NoAsistio  = "no_asistio";

    public static readonly string[] Todos = { Pendiente, Confirmada, Completada, Cancelada, NoAsistio };

    // Estados que ocupan el horario (restricción citas_sin_traslape_dentista)
    public static readonly string[] Activos = { Pendiente, Confirmada };
}

public static class OrigenesCita
{
    public const string Web       = "web";
    public const string Portal    = "portal";
    public const string Recepcion = "recepcion";

    public static readonly string[] Todos = { Web, Portal, Recepcion };
}
