namespace backend.Entities;

public class ExamenClinico : IConMarcasDeTiempo
{
    public int Id { get; set; }

    public int PacienteId { get; set; }

    // Consulta en la que se tomaron los signos vitales (opcional)
    public int? CitaId { get; set; }

    public string? Pa { get; set; }

    public int? FrecuenciaCardiaca { get; set; }

    public decimal? Temperatura { get; set; }

    public int? FrecuenciaRespiratoria { get; set; }

    public string? ExamenExtraoral { get; set; }

    public string? ExamenIntraoral { get; set; }

    public string? ResultadoExamenesAuxiliares { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual Paciente Paciente { get; set; } = null!;

    public virtual Cita? Cita { get; set; }
}
