namespace backend.Entities;

public class Oferta : IConMarcasDeTiempo, IEliminable
{
    public int Id { get; set; }

    public int ServicioId { get; set; }

    // Porcentaje (0, 100]
    public decimal Descuento { get; set; }

    // Fechas locales de la clínica, vigencia inclusiva
    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
