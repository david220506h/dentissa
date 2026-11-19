using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Cita
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int TrabajadorId { get; set; }

    public int ServicioId { get; set; }

    public int? OfertaId { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string? Estado { get; set; }

    public decimal? CostoBase { get; set; }

    public decimal? DescuentoAplicado { get; set; }

    public decimal? CostoExtra { get; set; }

    public string? MotivoExtra { get; set; }

    public decimal? CostoTotal { get; set; }

    public string? AgendadoPor { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<ConsultaRegistro> ConsultaRegistros { get; set; } = new List<ConsultaRegistro>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual Oferta? Oferta { get; set; }

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Trabajadore Trabajador { get; set; } = null!;
}
