namespace backend.Services.Agenda;

/// <summary>Canal por el que llega una cita: define la anticipación (diseño de la agenda, 1.5).</summary>
public enum CanalReserva
{
    /// <summary>Sitio y portal: anticipación en minutos de atención; la solicitud bloquea con vencimiento.</summary>
    EnLinea,

    /// <summary>Asistente y administración: anticipación en minutos de reloj; sin vencimiento.</summary>
    Recepcion,
}

public enum MotivoRechazo
{
    EnElPasado,
    FueraDeHorizonte,
    AnticipacionInsuficiente,
}

/// <summary>Parámetros de configuracion_agenda que usa la regla (en minutos y días).</summary>
public sealed record ParametrosBloqueo(
    int AnticipacionEnLineaAtencionMinutos,
    int AnticipacionRecepcionMinutos,
    int LiberacionAntesCitaAtencionMinutos,
    int VidaMaximaSolicitudAtencionMinutos,
    int HorizonteReservaDias);

/// <summary>
/// Resultado de CalcularBloqueo. Además de aceptada o rechazada, expone los valores intermedios (atención entre
/// solicitud y cita, vida, liberación) para que las pruebas comprueben cada columna de la tabla 2.5.
/// </summary>
public sealed record ResultadoBloqueo(
    bool Aceptada,
    MotivoRechazo? Motivo,
    DateTime? BloqueoExpiraEn,
    TimeSpan? Atencion,
    DateTime? Vida,
    DateTime? Liberacion)
{
    public static ResultadoBloqueo Rechazada(MotivoRechazo motivo, TimeSpan? atencion = null) => new(false, motivo, null, atencion, null, null);
}

/// <summary>
/// Regla de bloqueo de solicitudes (diseño de la agenda, sección 2; decisión E + A + D). Función pura y único
/// lugar donde vive la regla: la disponibilidad y la reserva la usan.
/// </summary>
public static class ReglaBloqueo
{
    public static ResultadoBloqueo CalcularBloqueo(DateTime creadoUtc, DateTime inicioUtc, CanalReserva canal, RelojAtencion reloj, ParametrosBloqueo p)
    {
        // Nunca en el pasado
        if (inicioUtc <= creadoUtc)
            return ResultadoBloqueo.Rechazada(MotivoRechazo.EnElPasado);

        // Horizonte: fecha local de la cita ≤ hoy local + horizonte
        var hoy = reloj.Horario.FechaLocal(creadoUtc);
        if (reloj.Horario.FechaLocal(inicioUtc) > hoy.AddDays(p.HorizonteReservaDias))
            return ResultadoBloqueo.Rechazada(MotivoRechazo.FueraDeHorizonte);

        if (canal == CanalReserva.Recepcion)
        {
            // Minutos de reloj; recepción no bloquea con vencimiento
            return inicioUtc - creadoUtc >= TimeSpan.FromMinutes(p.AnticipacionRecepcionMinutos)
                ? new ResultadoBloqueo(true, null, null, null, null, null)
                : ResultadoBloqueo.Rechazada(MotivoRechazo.AnticipacionInsuficiente);
        }

        var atencion = reloj.AtencionEntre(creadoUtc, inicioUtc);
        if (atencion < TimeSpan.FromMinutes(p.AnticipacionEnLineaAtencionMinutos))
            return ResultadoBloqueo.Rechazada(MotivoRechazo.AnticipacionInsuficiente, atencion);

        var vida = reloj.SumarAtencion(creadoUtc, TimeSpan.FromMinutes(p.VidaMaximaSolicitudAtencionMinutos));
        var liberacion = reloj.RestarAtencion(inicioUtc, TimeSpan.FromMinutes(p.LiberacionAntesCitaAtencionMinutos));
        var vence = vida < liberacion ? vida : liberacion;
        return new ResultadoBloqueo(true, null, vence, atencion, vida, liberacion);
    }
}
