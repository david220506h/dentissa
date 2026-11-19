using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class ServiciosRealizado
{
    public int Id { get; set; }

    public int ServicioId { get; set; }

    public int TrabajadorId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? FotoAntes { get; set; }

    public string? FotoDespues { get; set; }

    public DateTime? FechaRealizacion { get; set; }

    public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Trabajadore Trabajador { get; set; } = null!;
}
