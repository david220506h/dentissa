namespace backend.Entities;

public class Servicio : IConMarcasDeTiempo, IEliminable
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    // Horarios seguidos que ocupa; cada uno dura configuracion_agenda.duracion_slot_minutos
    public short DuracionSlots { get; set; } = 1;

    public string? Imagen { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual CategoriaServicio Categoria { get; set; } = null!;

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<Oferta> Ofertas { get; set; } = new List<Oferta>();

    public virtual ICollection<DentistaServicio> DentistaServicios { get; set; } = new List<DentistaServicio>();

    public virtual ICollection<ServiciosRealizado> ServiciosRealizados { get; set; } = new List<ServiciosRealizado>();
}
