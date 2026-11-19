using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Notificacione
{
    public int Id { get; set; }

    public int CitaId { get; set; }

    public int ClienteId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? Mensaje { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public bool? Enviada { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual Cliente Cliente { get; set; } = null!;
}
