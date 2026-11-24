using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    // Alta desde el panel: el origen es siempre "recepcion" y el usuario sale del token.
    // fecha + hora son hora local de la clínica.
    public class CitaDto
    {
        [Range(1, int.MaxValue)] public int PacienteId { get; set; }
        [Range(1, int.MaxValue)] public int DentistaId { get; set; }
        [Range(1, int.MaxValue)] public int ServicioId { get; set; }
        public DateOnly Fecha       { get; set; }
        public TimeOnly Hora        { get; set; }
        [Range(0, 999999)] public decimal? CostoExtra { get; set; }
        [MaxLength(300)] public string? MotivoExtra { get; set; }
        [MaxLength(500)] public string? Motivo      { get; set; }
        // Solo recepción (admin y asistente): el paciente está en la clínica; sin la anticipación de 60 minutos
        public bool PacientePresente { get; set; }
    }

    public class CitaEditDto
    {
        [Range(1, int.MaxValue)] public int DentistaId { get; set; }
        public DateOnly Fecha       { get; set; }
        public TimeOnly Hora        { get; set; }
        public string?  Estado      { get; set; }
        [Range(0, 999999)] public decimal? CostoExtra { get; set; }
        [MaxLength(300)] public string? MotivoExtra { get; set; }
    }

    // Respuesta de GET api/Citas y GET api/Citas/{id}: solo nombres, nunca
    // datos de la cuenta (correo, hash, tokens) ni datos personales del paciente
    public class CitaRespuestaDto
    {
        public int      Id                { get; set; }
        public string   Paciente          { get; set; } = string.Empty;
        public string   Servicio          { get; set; } = string.Empty;
        public string   Dentista          { get; set; } = string.Empty;
        // Hora local de la clínica
        public string   Fecha             { get; set; } = string.Empty;
        public string   Hora              { get; set; } = string.Empty;
        // ISO 8601 en UTC
        public DateTime Inicio            { get; set; }
        public DateTime Fin               { get; set; }
        public string   Estado            { get; set; } = string.Empty;
        public string   Origen            { get; set; } = string.Empty;
        public bool     PacientePresente  { get; set; }
        public string?  Motivo            { get; set; }
        public string?  MotivoCancelacion { get; set; }
        public SolicitudCambioDto? SolicitudCambio { get; set; }
        public decimal  CostoBase         { get; set; }
        public decimal  DescuentoAplicado { get; set; }
        public decimal  CostoExtra        { get; set; }
        public string?  MotivoExtra       { get; set; }
        public decimal  CostoTotal        { get; set; }
        public int      PacienteId        { get; set; }
        // La relación está eliminada lógicamente; la cita se muestra igual
        public bool     PacienteEliminado { get; set; }
        public bool     DentistaEliminado { get; set; }
        public bool     ServicioEliminado { get; set; }
        public int      DentistaId        { get; set; }
        public int      ServicioId        { get; set; }
        public decimal? Oferta            { get; set; }
    }

    public class CancelarCitaDto
    {
        [Required(ErrorMessage = "El motivo de cancelación es obligatorio")]
        [MaxLength(500)]
        public string? Motivo { get; set; }
    }

    public class SolicitudCambioDto
    {
        public string   Tipo   { get; set; } = string.Empty;
        public string?  Motivo { get; set; }
        public DateTime En     { get; set; }
    }

    public class PacienteOpcionDto
    {
        public int    Id       { get; set; }
        public string Nombre   { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
    }

    public class DentistaOpcionDto
    {
        public int     Id           { get; set; }
        public string  Nombre       { get; set; } = string.Empty;
        public string  Apellido     { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Titulo       { get; set; }
    }

    public class ServicioOpcionDto
    {
        public int     Id            { get; set; }
        public string  Nombre        { get; set; } = string.Empty;
        public decimal Precio        { get; set; }
        public short   DuracionSlots { get; set; }
    }
}
