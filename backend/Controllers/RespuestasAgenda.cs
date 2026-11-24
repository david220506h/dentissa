using backend.Repositories;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// Traducción única de las excepciones de la agenda a respuestas HTTP con { mensaje, codigo } (diseño, 4.2 y 6.3).
    /// </summary>
    public static class RespuestasAgenda
    {
        public static IActionResult? DesdeExcepcion(ControllerBase c, Exception ex) => ex switch
        {
            CitaNoEncontradaException e   => c.NotFound(new { mensaje = e.Message }),
            TransicionInvalidaException e => c.Conflict(new { mensaje = e.Message, codigo = "TRANSICION_INVALIDA" }),
            CitaModificadaException e     => c.Conflict(new { mensaje = e.Message, codigo = "CITA_MODIFICADA" }),
            HorarioOcupadoException e     => c.Conflict(new { mensaje = e.Message, codigo = "HORARIO_OCUPADO" }),
            ReservaRechazadaException e   => c.BadRequest(new { mensaje = e.Message, codigo = CodigoRechazo(e.Motivo) }),
            HoraFueraDeHorarioException e => c.BadRequest(new { mensaje = e.Message, codigo = "HORA_FUERA_DE_HORARIO" }),
            ServicioNoDisponibleException => c.BadRequest(new { mensaje = "Servicio no encontrado o inactivo" }),
            AgendaNoConfiguradaException e => c.Problem(e.Message, statusCode: StatusCodes.Status503ServiceUnavailable),
            ArgumentException e           => c.BadRequest(new { mensaje = e.Message }),
            _ => null
        };

        public static string CodigoRechazo(MotivoRechazo motivo) => motivo switch
        {
            MotivoRechazo.EnElPasado => "HORARIO_PASADO",
            MotivoRechazo.FueraDeHorizonte => "FUERA_DE_HORIZONTE",
            _ => "ANTICIPACION_INSUFICIENTE",
        };

        /// <summary>Ejecuta la acción y traduce las excepciones conocidas; las demás siguen su curso (500).</summary>
        public static async Task<IActionResult> EjecutarAsync(this ControllerBase c, Func<Task<IActionResult>> accion)
        {
            try
            {
                return await accion();
            }
            catch (Exception ex) when (DesdeExcepcion(c, ex) is { } respuesta)
            {
                return respuesta;
            }
        }
    }
}
