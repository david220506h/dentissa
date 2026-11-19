using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Servicio
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public int? Duracion { get; set; }

    public string? Imagen { get; set; }

    public bool? Disponible { get; set; }

    public virtual CategoriaServicio Categoria { get; set; } = null!;

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<Oferta> Oferta { get; set; } = new List<Oferta>();

    public virtual ICollection<ServiciosRealizado> ServiciosRealizados { get; set; } = new List<ServiciosRealizado>();

    public virtual ICollection<TrabajadorServicio> TrabajadorServicios { get; set; } = new List<TrabajadorServicio>();
}
