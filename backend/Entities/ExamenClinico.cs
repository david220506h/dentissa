using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class ExamenClinico
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string? Pa { get; set; }

    public int? FrecuenciaCardiaca { get; set; }

    public decimal? Temperatura { get; set; }

    public int? FrecuenciaRespiratoria { get; set; }

    public string? ExamenExtraoral { get; set; }

    public string? ExamenIntraoral { get; set; }

    public string? ResultadoExamenesAuxiliares { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;
}
