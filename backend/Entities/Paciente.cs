namespace backend.Entities;

// Un paciente puede existir sin cuenta (invitado que solicitó cita desde la web)
public class Paciente : IConMarcasDeTiempo, IEliminable
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    // Correo de contacto (avisos); el de inicio de sesión es usuarios.correo
    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? Sexo { get; set; }

    public string? Curp { get; set; }

    public string? Direccion { get; set; }

    public string? TipoSangre { get; set; }

    public string? Alergias { get; set; }

    public string? CondicionesMedicas { get; set; }

    public string Origen { get; set; } = null!;

    public bool ContactoVerificado { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual Usuario? Usuario { get; set; }

    public virtual ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public virtual ICollection<ExamenMedico> ExamenesMedicos { get; set; } = new List<ExamenMedico>();

    public virtual ICollection<ExamenClinico> ExamenesClinicos { get; set; } = new List<ExamenClinico>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
}
