using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Trabajadore
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string? Titulo { get; set; }

    public string? Especialidad { get; set; }

    public string? Descripcion { get; set; }

    public string? Foto { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<ServiciosRealizado> ServiciosRealizados { get; set; } = new List<ServiciosRealizado>();

    public virtual ICollection<TrabajadorServicio> TrabajadorServicios { get; set; } = new List<TrabajadorServicio>();

    public virtual Usuario Usuario { get; set; } = null!;
}
