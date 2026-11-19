using System;
using System.Collections.Generic;

namespace backend.Entities;

public partial class ConsultaFoto
{
    public int Id { get; set; }

    public int ConsultaRegistroId { get; set; }

    public string FotoUrl { get; set; } = null!;

    public DateTime? Fecha { get; set; }

    public virtual ConsultaRegistro ConsultaRegistro { get; set; } = null!;
}
