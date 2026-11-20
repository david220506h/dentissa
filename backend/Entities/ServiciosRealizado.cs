namespace backend.Entities;

public class ServiciosRealizado
{
    public int Id { get; set; }

    public int ServicioId { get; set; }

    public int DentistaId { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? FotoAntes { get; set; }

    public string? FotoDespues { get; set; }

    public DateTime? FechaRealizacion { get; set; }

    public virtual Servicio Servicio { get; set; } = null!;

    public virtual Dentista Dentista { get; set; } = null!;
}
