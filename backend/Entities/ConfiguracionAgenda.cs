namespace backend.Entities;

// Una sola fila (id = 1). Los valores iniciales los crea el servicio de arranque.
public class ConfiguracionAgenda
{
    public const int IdUnico = 1;

    public int Id { get; set; } = IdUnico;

    public short DuracionSlotMinutos { get; set; }

    public short AnticipacionMinimaHoras { get; set; }

    public short BloqueoPendienteHoras { get; set; }

    public short LiberacionAntesCitaHoras { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public int? ActualizadoPor { get; set; }

    public virtual Usuario? ActualizadoPorUsuario { get; set; }

    public virtual ICollection<HorarioDia> Dias { get; set; } = new List<HorarioDia>();

    public virtual ICollection<HorarioDescanso> Descansos { get; set; } = new List<HorarioDescanso>();

    public virtual ICollection<AgendaCierre> Cierres { get; set; } = new List<AgendaCierre>();
}

// dia_semana: 1 = lunes … 7 = domingo (ISO 8601); horas locales de la clínica
public class HorarioDia
{
    public int Id { get; set; }

    public int ConfiguracionId { get; set; }

    public short DiaSemana { get; set; }

    public bool Laborable { get; set; }

    public TimeOnly? HoraApertura { get; set; }

    public TimeOnly? HoraCierre { get; set; }

    public virtual ConfiguracionAgenda Configuracion { get; set; } = null!;
}

// dia_semana nulo = todos los días laborables
public class HorarioDescanso
{
    public int Id { get; set; }

    public int ConfiguracionId { get; set; }

    public short? DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public virtual ConfiguracionAgenda Configuracion { get; set; } = null!;
}

// Festivos y cierres; sin horas = todo el día
public class AgendaCierre
{
    public int Id { get; set; }

    public int ConfiguracionId { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly? HoraInicio { get; set; }

    public TimeOnly? HoraFin { get; set; }

    public string? Motivo { get; set; }

    public DateTime CreadoEn { get; set; }

    public int? CreadoPor { get; set; }

    public virtual ConfiguracionAgenda Configuracion { get; set; } = null!;

    public virtual Usuario? CreadoPorUsuario { get; set; }
}
