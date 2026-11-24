using backend.Entities;
using backend.Services;

namespace backend.DTOs
{
    // Una sola conversión de Cita a su DTO, compartida por el panel y por mi-agenda
    public static class CitaMapper
    {
        public static CitaRespuestaDto ARespuesta(Cita c, IRelojClinica reloj)
        {
            var (fecha, hora) = reloj.ALocal(c.Inicio);
            return new CitaRespuestaDto
            {
                Id                = c.Id,
                Paciente          = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                Servicio          = c.Servicio.Nombre,
                Dentista          = c.Dentista.Usuario.Nombre + " " + c.Dentista.Usuario.Apellido,
                Fecha             = fecha.ToString("dd/MM/yyyy"),
                Hora              = hora.ToString("HH:mm"),
                Inicio            = DateTime.SpecifyKind(c.Inicio, DateTimeKind.Utc),
                Fin               = DateTime.SpecifyKind(c.Fin, DateTimeKind.Utc),
                Estado            = c.Estado,
                Origen            = c.Origen,
                PacientePresente  = c.PacientePresente,
                Version           = c.Version,
                Motivo            = c.Motivo,
                MotivoCancelacion = c.MotivoCancelacion,
                SolicitudCambio   = c.SolicitudCambioTipo == null ? null : new SolicitudCambioDto
                {
                    Tipo   = c.SolicitudCambioTipo,
                    Motivo = c.SolicitudCambioMotivo,
                    En     = DateTime.SpecifyKind(c.SolicitudCambioEn!.Value, DateTimeKind.Utc)
                },
                CostoBase         = c.CostoBase,
                DescuentoAplicado = c.DescuentoAplicado,
                CostoExtra        = c.CostoExtra,
                MotivoExtra       = c.MotivoExtra,
                CostoTotal        = c.CostoTotal,
                PacienteId        = c.PacienteId,
                PacienteEliminado = c.Paciente.EliminadoEn != null,
                DentistaEliminado = c.Dentista.EliminadoEn != null,
                ServicioEliminado = c.Servicio.EliminadoEn != null,
                DentistaId        = c.DentistaId,
                ServicioId        = c.ServicioId,
                Oferta            = c.Oferta?.Descuento
            };
        }
    }
}
