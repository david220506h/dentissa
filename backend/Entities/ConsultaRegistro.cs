using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class ConsultaRegistro
{
    public int Id { get; set; }

    public int CitaId { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? Fecha { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual ICollection<ConsultaFoto> ConsultaFotos { get; set; } = new List<ConsultaFoto>();
}
