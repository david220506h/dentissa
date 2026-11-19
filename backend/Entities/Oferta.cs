using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Oferta
{
    public int Id { get; set; }

    public int ServicioId { get; set; }

    public decimal Descuento { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool? Activa { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual Servicio Servicio { get; set; } = null!;
}
