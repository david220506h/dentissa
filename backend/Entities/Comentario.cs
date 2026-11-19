using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Comentario
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int ServicioRealizadoId { get; set; }

    public string Comentario1 { get; set; } = null!;

    public int? Calificacion { get; set; }

    public DateTime? Fecha { get; set; }

    public bool? Visible { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ServiciosRealizado ServicioRealizado { get; set; } = null!;
}
