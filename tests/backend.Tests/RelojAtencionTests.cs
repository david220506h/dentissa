using backend.Services.Agenda;

namespace backend.Tests;

// Fase 2, commit 3 (diseño de la agenda, sección 2): reloj de atención y CalcularBloqueo, como funciones
// puras. La tabla 2.5 se prueba completa, columna por columna.
public class RelojAtencionTests
{
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    // Configuración sembrada: lunes a sábado 8–16, descansos 11–12 y 14–15
    public static HorarioAtencion Horario(params HorarioAtencion.Cierre[] cierres) => new(
        Enumerable.Range(1, 6).Select(d => new HorarioAtencion.Dia((short)d, new TimeOnly(8, 0), new TimeOnly(16, 0))),
        [new HorarioAtencion.Descanso(null, new TimeOnly(11, 0), new TimeOnly(12, 0)), new HorarioAtencion.Descanso(null, new TimeOnly(14, 0), new TimeOnly(15, 0))],
        cierres,
        Zona);

    public static readonly ParametrosBloqueo Parametros = new(180, 60, 120, 720, 60);

    // "sáb 28-nov 20:00" (hora local de la clínica) → UTC
    public static DateTime L(string texto) => L(Horario(), texto);

    private static DateTime L(HorarioAtencion h, string texto)
    {
        var f = DateTime.ParseExact(texto, "yyyy-MM-dd HH:mm", null);
        return h.AUtc(DateOnly.FromDateTime(f), TimeOnly.FromDateTime(f));
    }

    private static readonly HorarioAtencion.Cierre Navidad = new(new DateOnly(2026, 12, 25), null, null);
    private static readonly HorarioAtencion.Cierre TardeMiercoles = new(new DateOnly(2026, 12, 2), new TimeOnly(12, 0), new TimeOnly(16, 0));

    [Fact]
    public void Tramos_DelLunes_SonTresConLaConfiguracionSembrada()
    {
        var tramos = Horario().IntervalosLocales(new DateOnly(2026, 11, 30));

        Assert.Equal([(new TimeOnly(8, 0), new TimeOnly(11, 0)), (new TimeOnly(12, 0), new TimeOnly(14, 0)), (new TimeOnly(15, 0), new TimeOnly(16, 0))], tramos);
        Assert.Empty(Horario().IntervalosLocales(new DateOnly(2026, 11, 29))); // domingo
        Assert.Empty(Horario(Navidad).IntervalosLocales(new DateOnly(2026, 12, 25)));
        Assert.Equal([(new TimeOnly(8, 0), new TimeOnly(11, 0))], Horario(TardeMiercoles).IntervalosLocales(new DateOnly(2026, 12, 2)));
    }

    [Fact]
    public void Convenciones_DeLosLimites()
    {
        var reloj = new RelojAtencion(Horario());

        // Sumar que termina justo al cerrar un tramo: ese cierre, no la siguiente apertura
        Assert.Equal(L("2026-12-01 11:00"), reloj.SumarAtencion(L("2026-12-01 08:00"), TimeSpan.FromHours(3)));
        // Restar con un descanso en medio: el instante más cercano
        Assert.Equal(L("2026-12-01 12:00"), reloj.RestarAtencion(L("2026-12-01 15:00"), TimeSpan.FromHours(2)));
        // Desde dentro de un descanso
        Assert.Equal(TimeSpan.FromHours(2), reloj.AtencionEntre(L("2026-12-01 11:30"), L("2026-12-01 15:00")));
        // Cero
        Assert.Equal(L("2026-12-01 09:00"), reloj.SumarAtencion(L("2026-12-01 09:00"), TimeSpan.Zero));
        Assert.Equal(TimeSpan.Zero, reloj.AtencionEntre(L("2026-12-01 10:00"), L("2026-12-01 09:00")));
    }

    [Fact]
    public void SinDiasLaborables_ErrorControlado()
    {
        var sinAtencion = new HorarioAtencion([], [], [], Zona);
        var reloj = new RelojAtencion(sinAtencion);

        Assert.Throws<CalendarioSinAtencionException>(() => reloj.SumarAtencion(L("2026-12-01 08:00"), TimeSpan.FromHours(1)));
        Assert.Throws<CalendarioSinAtencionException>(() => reloj.RestarAtencion(L("2026-12-01 08:00"), TimeSpan.FromHours(1)));
    }

    // Tabla 2.5 (aprobada). Columnas: creado, inicio, canal, cierres, atención (h), aceptada, motivo, vida, liberación, vence, ventana (h)
    public static IEnumerable<object?[]> Escenarios => new object?[][]
    {
        new object?[] { 1, "2026-11-28 20:00", "2026-11-30 08:00", CanalReserva.EnLinea, "", 0.0, false, MotivoRechazo.AnticipacionInsuficiente, null, null, null, null },
        new object?[] { 2, "2026-11-28 20:00", "2026-11-30 12:00", CanalReserva.EnLinea, "", 3.0, true, null, "2026-12-01 16:00", "2026-11-30 09:00", "2026-11-30 09:00", 1.0 },
        new object?[] { 3, "2026-12-01 08:00", "2026-12-01 15:00", CanalReserva.EnLinea, "", 5.0, true, null, "2026-12-02 16:00", "2026-12-01 12:00", "2026-12-01 12:00", 3.0 },
        new object?[] { 4, "2026-12-01 08:00", "2026-12-01 10:00", CanalReserva.EnLinea, "", 2.0, false, MotivoRechazo.AnticipacionInsuficiente, null, null, null, null },
        new object?[] { 5, "2026-12-01 09:00", "2026-12-01 13:00", CanalReserva.EnLinea, "", 3.0, true, null, "2026-12-03 09:00", "2026-12-01 10:00", "2026-12-01 10:00", 1.0 },
        new object?[] { 6, "2026-12-01 11:30", "2026-12-01 15:00", CanalReserva.EnLinea, "", 2.0, false, MotivoRechazo.AnticipacionInsuficiente, null, null, null, null },
        new object?[] { 7, "2026-12-05 09:00", "2026-12-07 10:00", CanalReserva.EnLinea, "", 7.0, true, null, "2026-12-08 09:00", "2026-12-07 08:00", "2026-12-07 08:00", 5.0 },
        new object?[] { 8, "2026-12-04 15:30", "2026-12-07 09:00", CanalReserva.EnLinea, "", 7.5, true, null, "2026-12-07 15:30", "2026-12-05 15:00", "2026-12-05 15:00", 5.5 },
        new object?[] { 9, "2026-11-30 08:00", "2026-12-14 08:00", CanalReserva.EnLinea, "", 72.0, true, null, "2026-12-01 16:00", "2026-12-12 13:00", "2026-12-01 16:00", 12.0 },
        new object?[] { 10, "2026-12-24 10:00", "2026-12-26 10:00", CanalReserva.EnLinea, "navidad", 6.0, true, null, "2026-12-28 10:00", "2026-12-26 08:00", "2026-12-26 08:00", 4.0 },
        new object?[] { 11, "2026-12-24 15:00", "2026-12-26 09:00", CanalReserva.EnLinea, "navidad", 2.0, false, MotivoRechazo.AnticipacionInsuficiente, null, null, null, null },
        new object?[] { 12, "2026-12-02 08:00", "2026-12-03 09:00", CanalReserva.EnLinea, "tarde", 4.0, true, null, "2026-12-04 11:00", "2026-12-02 10:00", "2026-12-02 10:00", 2.0 },
        new object?[] { 13, "2026-12-01 14:00", "2026-12-01 15:00", CanalReserva.Recepcion, "", null, true, null, null, null, null, null },
        new object?[] { 14, "2026-12-01 14:30", "2026-12-01 15:00", CanalReserva.Recepcion, "", null, false, MotivoRechazo.AnticipacionInsuficiente, null, null, null, null },
        new object?[] { 15, "2026-11-30 10:00", "2027-02-02 08:00", CanalReserva.EnLinea, "", null, false, MotivoRechazo.FueraDeHorizonte, null, null, null, null },
    };

    [Theory]
    [MemberData(nameof(Escenarios))]
    public void Tabla25(int fila, string creado, string inicio, CanalReserva canal, string cierres, double? atencionHoras,
        bool aceptada, MotivoRechazo? motivo, string? vida, string? liberacion, string? vence, double? ventanaHoras)
    {
        var horario = cierres switch { "navidad" => Horario(Navidad), "tarde" => Horario(TardeMiercoles), _ => Horario() };
        var reloj = new RelojAtencion(horario);
        var c = L(horario, creado);
        var i = L(horario, inicio);

        var r = ReglaBloqueo.CalcularBloqueo(c, i, canal, reloj, Parametros);

        Assert.True(aceptada == r.Aceptada, $"fila {fila}: aceptada");
        Assert.Equal(motivo, r.Motivo);
        if (atencionHoras is { } horas)
            Assert.Equal(TimeSpan.FromHours(horas), reloj.AtencionEntre(c, i));
        Assert.Equal(vida is null ? null : L(horario, vida), r.Vida);
        Assert.Equal(liberacion is null ? null : L(horario, liberacion), r.Liberacion);
        Assert.Equal(vence is null ? null : L(horario, vence), r.BloqueoExpiraEn);
        if (ventanaHoras is { } ventana)
            Assert.Equal(TimeSpan.FromHours(ventana), reloj.AtencionEntre(c, r.BloqueoExpiraEn!.Value));
        if (canal == CanalReserva.Recepcion)
            Assert.Null(r.BloqueoExpiraEn);
    }

    [Fact]
    public void Fila4_RecepcionSiPuede()
    {
        var reloj = new RelojAtencion(Horario());

        var r = ReglaBloqueo.CalcularBloqueo(L("2026-12-01 08:00"), L("2026-12-01 10:00"), CanalReserva.Recepcion, reloj, Parametros);

        Assert.True(r.Aceptada);
        Assert.Null(r.BloqueoExpiraEn);
    }

    [Fact]
    public void NuncaEnElPasado()
    {
        var reloj = new RelojAtencion(Horario());

        var r = ReglaBloqueo.CalcularBloqueo(L("2026-12-01 10:00"), L("2026-12-01 09:00"), CanalReserva.Recepcion, reloj, Parametros);

        Assert.Equal(MotivoRechazo.EnElPasado, r.Motivo);
    }

    // Propiedad (diseño, 2.3): toda solicitud aceptada vence después de crearse, con una ventana de entre 1 h y
    // 12 h de atención. Fechas aleatorias con semilla fija (reproducible).
    [Fact]
    public void Propiedad_VentanaEntreUnaYDoceHorasDeAtencion()
    {
        var reloj = new RelojAtencion(Horario(Navidad, TardeMiercoles));
        var azar = new Random(20261123);
        var origen = L("2026-11-23 00:00");
        var aceptadas = 0;

        for (var n = 0; n < 2000; n++)
        {
            var creado = origen.AddMinutes(azar.Next(0, 60 * 24 * 45));
            var inicio = creado.AddMinutes(azar.Next(1, 60 * 24 * 20));
            var r = ReglaBloqueo.CalcularBloqueo(creado, inicio, CanalReserva.EnLinea, reloj, Parametros);
            if (!r.Aceptada) continue;
            aceptadas++;

            Assert.True(r.BloqueoExpiraEn > creado, $"vence después de crearse ({creado:o} → {inicio:o})");
            var ventana = reloj.AtencionEntre(creado, r.BloqueoExpiraEn!.Value);
            Assert.InRange(ventana, TimeSpan.FromHours(1), TimeSpan.FromHours(12));
        }

        Assert.True(aceptadas > 500, "la muestra incluye suficientes solicitudes aceptadas");
    }
}
