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
}