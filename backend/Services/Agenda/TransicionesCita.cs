using backend.Entities;

namespace backend.Services.Agenda;

/// <summary>La transición pedida no es válida desde el estado actual o todavía no se puede (409 TRANSICION_INVALIDA).</summary>
public class TransicionInvalidaException : InvalidOperationException
{
    public TransicionInvalidaException(string mensaje) : base(mensaje) { }
}

/// <summary>La cita no existe o no es de quien la pide (404).</summary>
public class CitaNoEncontradaException : InvalidOperationException
{
    public CitaNoEncontradaException() : base("Cita no encontrada") { }
}

/// <summary>Otra persona cambió la cita entre la lectura y el guardado (409 CITA_MODIFICADA).</summary>
public class CitaModificadaException : InvalidOperationException
{
    public const string Mensaje = "La cita cambió mientras la editabas; recarga.";

    public CitaModificadaException(Exception? interna = null) : base(Mensaje, interna) { }
}

/// <summary>
/// Única clase que valida las transiciones de estado de una cita (diseño de la agenda, 4.2). Función pura: recibe la
/// cita y el instante; los servicios y controladores no repiten estas reglas.
/// </summary>
public static class TransicionesCita
{
    /// <summary>"No asistió" se puede marcar desde inicio + 15 minutos (decisión 7).</summary>
    public const int MinutosParaNoAsistio = 15;

    /// <summary>La administración corrige completada ↔ no asistió hasta 7 días después del inicio (decisión 7).</summary>
    public const int DiasParaCorregir = 7;

    public static readonly string[] EstadosCorregibles = { EstadosCita.Completada, EstadosCita.NoAsistio };

    private static bool Activa(Cita c) => EstadosCita.Activos.Contains(c.Estado);

    /// <summary>Devuelve false si ya estaba confirmada (sin cambios); lanza si no se puede confirmar.</summary>
    public static bool ValidarConfirmar(Cita c)
    {
        if (c.Estado == EstadosCita.Confirmada) return false;
        if (c.Estado != EstadosCita.Pendiente)
            throw new TransicionInvalidaException("Solo se puede confirmar una cita pendiente.");
        return true;
    }

    public static void ValidarCancelar(Cita c)
    {
        if (!Activa(c))
            throw new TransicionInvalidaException("Solo se puede cancelar una cita pendiente o confirmada.");
    }

    public static void ValidarCompletar(Cita c, DateTime ahora)
    {
        if (!Activa(c))
            throw new TransicionInvalidaException("Solo se puede completar una cita pendiente o confirmada.");
        if (ahora < c.Inicio)
            throw new TransicionInvalidaException("La cita todavía no empieza.");
    }

    public static void ValidarNoAsistio(Cita c, DateTime ahora)
    {
        if (!Activa(c))
            throw new TransicionInvalidaException("Solo se puede marcar la inasistencia de una cita pendiente o confirmada.");
        if (ahora < c.Inicio.AddMinutes(MinutosParaNoAsistio))
            throw new TransicionInvalidaException($"La inasistencia se marca {MinutosParaNoAsistio} minutos después del inicio.");
    }

    public static void ValidarCorreccion(Cita c, string hacia, DateTime ahora)
    {
        if (!EstadosCorregibles.Contains(hacia))
            throw new TransicionInvalidaException("Solo se corrige entre completada y no asistió.");
        if (!EstadosCorregibles.Contains(c.Estado) || c.Estado == hacia)
            throw new TransicionInvalidaException("Solo se corrige una cita completada o marcada como no asistió, hacia el otro estado.");
        if (ahora > c.Inicio.AddDays(DiasParaCorregir))
            throw new TransicionInvalidaException($"La corrección solo es posible hasta {DiasParaCorregir} días después de la cita.");
    }

    public static void ValidarReagenda(Cita c, string estadoResultante)
    {
        if (!Activa(c))
            throw new TransicionInvalidaException("Solo se reagenda una cita pendiente o confirmada.");
        if (!EstadosCita.Activos.Contains(estadoResultante))
            throw new TransicionInvalidaException("Una cita reagendada queda confirmada o pendiente.");
    }

    public static void ValidarReasignacion(Cita c, int dentistaNuevo)
    {
        if (!Activa(c))
            throw new TransicionInvalidaException("Solo se reasigna una cita pendiente o confirmada.");
        if (c.DentistaId == dentistaNuevo)
            throw new TransicionInvalidaException("La cita ya está asignada a ese dentista.");
    }

    public static void ValidarCostos(Cita c)
    {
        if (c.Estado == EstadosCita.Cancelada)
            throw new TransicionInvalidaException("Una cita cancelada no cambia de costos.");
    }
}
