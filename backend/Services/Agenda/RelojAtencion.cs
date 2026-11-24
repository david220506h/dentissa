namespace backend.Services.Agenda;

/// <summary>Tramo de atención [Inicio, Fin) en UTC.</summary>
public readonly record struct Tramo(DateTime InicioUtc, DateTime FinUtc)
{
    public TimeSpan Duracion => FinUtc - InicioUtc;
}

/// <summary>La configuración no tiene días con atención: las operaciones no pueden terminar.</summary>
public class CalendarioSinAtencionException : InvalidOperationException
{
    public CalendarioSinAtencionException()
        : base("La agenda no tiene horario de atención en el próximo año. Revisa los días laborables y los cierres.") { }
}

/// <summary>
/// Horario de atención de la clínica (diseño de la agenda, 2.2), sin base de datos: días laborables con su
/// apertura y cierre, descansos (de todos los días o de uno) y cierres (de día completo o parciales), en
/// hora local. Produce los tramos de atención de cada fecha, convertidos a UTC con la zona IANA.
/// </summary>
public sealed class HorarioAtencion
{
    public record Dia(short DiaSemana, TimeOnly Apertura, TimeOnly Cierre);
    public record Descanso(short? DiaSemana, TimeOnly Inicio, TimeOnly Fin);
    public record Cierre(DateOnly Fecha, TimeOnly? Inicio, TimeOnly? Fin);

    private readonly Dictionary<short, Dia> _dias;
    private readonly IReadOnlyList<Descanso> _descansos;
    private readonly ILookup<DateOnly, Cierre> _cierres;

    public HorarioAtencion(IEnumerable<Dia> diasLaborables, IEnumerable<Descanso> descansos, IEnumerable<Cierre> cierres, TimeZoneInfo zona)
    {
        _dias = diasLaborables.ToDictionary(d => d.DiaSemana);
        _descansos = descansos.ToList();
        _cierres = cierres.ToLookup(c => c.Fecha);
        Zona = zona;
    }

    public TimeZoneInfo Zona { get; }

    /// <summary>Día ISO: 1 lunes … 7 domingo.</summary>
    public static short DiaIso(DateOnly fecha) => (short)(fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek);

    public DateOnly FechaLocal(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona));

    /// <summary>
    /// Hora local → UTC. Una hora inexistente (salto de horario de verano) se mueve al primer instante válido;
    /// una ambigua toma la primera ocurrencia (diseño, 1.3.6).
    /// </summary>
    public DateTime AUtc(DateOnly fecha, TimeOnly hora)
    {
        var local = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
        while (Zona.IsInvalidTime(local))
            local = local.AddMinutes(1);
        if (Zona.IsAmbiguousTime(local))
        {
            var mayor = Zona.GetAmbiguousTimeOffsets(local).Max();
            return DateTime.SpecifyKind(local - mayor, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, Zona);
    }

    /// <summary>Intervalos locales de atención de una fecha: apertura a cierre, menos descansos y cierres.</summary>
    public IReadOnlyList<(TimeOnly Inicio, TimeOnly Fin)> IntervalosLocales(DateOnly fecha)
    {
        if (!_dias.TryGetValue(DiaIso(fecha), out var dia) || dia.Cierre <= dia.Apertura)
            return Array.Empty<(TimeOnly, TimeOnly)>();
        var cierres = _cierres[fecha].ToList();
        if (cierres.Any(c => c.Inicio is null))
            return Array.Empty<(TimeOnly, TimeOnly)>();

        var huecos = _descansos.Where(d => d.DiaSemana is null || d.DiaSemana == dia.DiaSemana).Select(d => (d.Inicio, d.Fin))
            .Concat(cierres.Select(c => (c.Inicio!.Value, c.Fin!.Value)))
            .OrderBy(h => h.Item1)
            .ToList();

        // Resta de intervalos: se recorre de la apertura al cierre saltando cada hueco
        var tramos = new List<(TimeOnly, TimeOnly)>();
        var cursor = dia.Apertura;
        foreach (var (inicio, fin) in huecos)
        {
            if (fin <= cursor) continue;
            if (inicio >= dia.Cierre) break;
            if (inicio > cursor) tramos.Add((cursor, inicio));
            cursor = fin;
            if (cursor >= dia.Cierre) break;
        }
        if (cursor < dia.Cierre) tramos.Add((cursor, dia.Cierre));
        return tramos;
    }

    public IReadOnlyList<Tramo> TramosDelDia(DateOnly fecha) =>
        IntervalosLocales(fecha).Select(t => new Tramo(AUtc(fecha, t.Inicio), AUtc(fecha, t.Fin))).ToList();
}

/// <summary>
/// Reloj de atención (diseño de la agenda, 2.2): cuenta solo el tiempo en que la clínica atiende. Clase pura:
/// sin base ni reloj del sistema. Las búsquedas tienen un tope de 366 días.
/// </summary>
public sealed class RelojAtencion
{
    public const int TopeDias = 366;
    private readonly HorarioAtencion _horario;
    private readonly Dictionary<DateOnly, IReadOnlyList<Tramo>> _cache = new();

    public RelojAtencion(HorarioAtencion horario) => _horario = horario;

    public HorarioAtencion Horario => _horario;

    private IReadOnlyList<Tramo> Tramos(DateOnly fecha)
    {
        if (!_cache.TryGetValue(fecha, out var tramos))
            _cache[fecha] = tramos = _horario.TramosDelDia(fecha);
        return tramos;
    }

    // Los días locales que tocan un instante UTC incluyen el anterior y el siguiente, por la diferencia de zona
    private DateOnly Fecha(DateTime utc) => _horario.FechaLocal(utc);

    /// <summary>Tiempo de atención dentro de [a, b).</summary>
    public TimeSpan AtencionEntre(DateTime a, DateTime b)
    {
        if (b <= a) return TimeSpan.Zero;
        var total = TimeSpan.Zero;
        for (var f = Fecha(a).AddDays(-1); f <= Fecha(b).AddDays(1); f = f.AddDays(1))
            foreach (var t in Tramos(f))
            {
                var inicio = t.InicioUtc > a ? t.InicioUtc : a;
                var fin = t.FinUtc < b ? t.FinUtc : b;
                if (fin > inicio) total += fin - inicio;
            }
        return total;
    }

    /// <summary>
    /// El primer instante t' ≥ t con AtencionEntre(t, t') = m. Si m se completa justo al cerrar un tramo, el
    /// resultado es ese cierre (8:00 + 3 h = 11:00, no 12:00).
    /// </summary>
    public DateTime SumarAtencion(DateTime t, TimeSpan m)
    {
        if (m <= TimeSpan.Zero) return t;
        var restante = m;
        var inicioBusqueda = Fecha(t).AddDays(-1);
        for (var f = inicioBusqueda; f <= inicioBusqueda.AddDays(TopeDias + 1); f = f.AddDays(1))
            foreach (var tramo in Tramos(f))
            {
                if (tramo.FinUtc <= t) continue;
                var desde = tramo.InicioUtc > t ? tramo.InicioUtc : t;
                var disponible = tramo.FinUtc - desde;
                if (restante <= disponible) return desde + restante;
                restante -= disponible;
            }
        throw new CalendarioSinAtencionException();
    }

    /// <summary>
    /// El último instante t' ≤ t con AtencionEntre(t', t) = m. Si varios cumplen (hay un descanso en medio),
    /// el más cercano a t (15:00 − 2 h = 12:00, no 11:00).
    /// </summary>
    public DateTime RestarAtencion(DateTime t, TimeSpan m)
    {
        if (m <= TimeSpan.Zero) return t;
        var restante = m;
        var finBusqueda = Fecha(t).AddDays(1);
        for (var f = finBusqueda; f >= finBusqueda.AddDays(-(TopeDias + 1)); f = f.AddDays(-1))
        {
            var tramos = Tramos(f);
            for (var i = tramos.Count - 1; i >= 0; i--)
            {
                var tramo = tramos[i];
                if (tramo.InicioUtc >= t) continue;
                var hasta = tramo.FinUtc < t ? tramo.FinUtc : t;
                var disponible = hasta - tramo.InicioUtc;
                if (restante <= disponible) return hasta - restante;
                restante -= disponible;
            }
        }
        throw new CalendarioSinAtencionException();
    }
}
