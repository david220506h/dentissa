using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class TrabajadorServicio
{
    public int Id { get; set; }

    public int TrabajadorId { get; set; }

    public int ServicioId { get; set; }

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Trabajadore Trabajador { get; set; } = null!;
}
