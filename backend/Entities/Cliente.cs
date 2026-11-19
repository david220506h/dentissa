using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class Cliente
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string? Curp { get; set; }

    public string? Sexo { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();

    public virtual ICollection<ExamenClinico> ExamenClinicos { get; set; } = new List<ExamenClinico>();

    public virtual ICollection<ExamenMedico> ExamenMedicos { get; set; } = new List<ExamenMedico>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual Usuario Usuario { get; set; } = null!;
}
