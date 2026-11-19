namespace backend.DTOs
{
    public class CitaDto
    {
        public int      ClienteId    { get; set; }
        public int      TrabajadorId { get; set; }
        public int      ServicioId   { get; set; }
        public DateOnly Fecha        { get; set; }
        public TimeOnly Hora         { get; set; }
        public decimal? CostoExtra   { get; set; }
        public string?  MotivoExtra  { get; set; }
        public string?  AgendadoPor  { get; set; }
    }

    public class CitaEditDto
    {
        public int      TrabajadorId { get; set; }
        public DateOnly Fecha        { get; set; }
        public TimeOnly Hora         { get; set; }
        public string?  Estado       { get; set; }
        public decimal? CostoExtra   { get; set; }
        public string?  MotivoExtra  { get; set; }
    }

    // Respuesta de GET api/Citas y GET api/Citas/{id}: solo nombres, nunca
    // datos de la cuenta (correo, hash, tokens) ni datos personales del paciente
    public class CitaRespuestaDto
    {
        public int      Id                { get; set; }
        public string   Paciente          { get; set; } = string.Empty;
        public string   Servicio          { get; set; } = string.Empty;
        public string   Trabajador        { get; set; } = string.Empty;
        public string   Fecha             { get; set; } = string.Empty;
        public string   Hora              { get; set; } = string.Empty;
        public string?  Estado            { get; set; }
        public decimal? CostoBase         { get; set; }
        public decimal? DescuentoAplicado { get; set; }
        public decimal? CostoExtra        { get; set; }
        public string?  MotivoExtra       { get; set; }
        public decimal? CostoTotal        { get; set; }
        public string?  AgendadoPor       { get; set; }
        public int      ClienteId         { get; set; }
        public int      TrabajadorId      { get; set; }
        public int      ServicioId        { get; set; }
        public decimal? Oferta            { get; set; }
    }

    public class ClienteOpcionDto
    {
        public int    Id       { get; set; }
        public string Nombre   { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
    }

    public class TrabajadorOpcionDto
    {
        public int     Id           { get; set; }
        public string  Nombre       { get; set; } = string.Empty;
        public string  Apellido     { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Titulo       { get; set; }
    }

    public class ServicioOpcionDto
    {
        public int     Id       { get; set; }
        public string  Nombre   { get; set; } = string.Empty;
        public decimal Precio   { get; set; }
        public int?    Duracion { get; set; }
    }
}