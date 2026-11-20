namespace backend.Entities;

public class Dentista : IConMarcasDeTiempo, IEliminable
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string? CedulaProfesional { get; set; }

    public string? Especialidad { get; set; }

    public string? Titulo { get; set; }

    public string? Descripcion { get; set; }

    public string? Foto { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<DentistaServicio> DentistaServicios { get; set; } = new List<DentistaServicio>();

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<ServiciosRealizado> ServiciosRealizados { get; set; } = new List<ServiciosRealizado>();
}

public class DentistaServicio
{
    public int DentistaId { get; set; }

    public int ServicioId { get; set; }

    public virtual Dentista Dentista { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
}
