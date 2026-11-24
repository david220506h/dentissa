using backend.Entities;
using backend.Services.Agenda;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

// Fase 2, commit 3: el calendario lee de la base el horario sembrado, los cierres y los parámetros
public class CalendarioAtencionTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public CalendarioAtencionTests(DentissaFactory factory) => _factory = factory;

    private async Task<T> ConCalendarioAsync<T>(Func<ICalendarioAtencion, Task<T>> accion)
    {
        using var scope = _factory.Services.CreateScope();
        return await accion(scope.ServiceProvider.GetRequiredService<ICalendarioAtencion>());
    }

    [Fact]
    public async Task HorarioSembrado_LunesTieneTresTramos_YDomingoNinguno()
    {
        var horario = await ConCalendarioAsync(c => c.ObtenerHorarioAsync());

        Assert.Equal(
            [(new TimeOnly(8, 0), new TimeOnly(11, 0)), (new TimeOnly(12, 0), new TimeOnly(14, 0)), (new TimeOnly(15, 0), new TimeOnly(16, 0))],
            horario.IntervalosLocales(new DateOnly(2031, 3, 3)));
        Assert.Empty(horario.IntervalosLocales(new DateOnly(2031, 3, 2)));
    }

    [Fact]
    public async Task CierresDeLaBase_SeAplican()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var cierreTotal = hoy.AddDays(30);
        var cierreParcial = hoy.AddDays(31);
        await _factory.ConDbAsync(db =>
        {
            db.AgendaCierres.Add(new AgendaCierre { ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = cierreTotal, Motivo = "Prueba" });
            db.AgendaCierres.Add(new AgendaCierre
            {
                ConfiguracionId = ConfiguracionAgenda.IdUnico, Fecha = cierreParcial,
                HoraInicio = new TimeOnly(12, 0), HoraFin = new TimeOnly(16, 0), Motivo = "Prueba"
            });
            return db.SaveChangesAsync();
        });

        var horario = await ConCalendarioAsync(c => c.ObtenerHorarioAsync());

        Assert.Empty(horario.IntervalosLocales(cierreTotal));
        if (HorarioAtencion.DiaIso(cierreParcial) != 7)
            Assert.DoesNotContain(horario.IntervalosLocales(cierreParcial), t => t.Fin > new TimeOnly(12, 0));
    }

    [Fact]
    public async Task Parametros_SonLosDeLaConfiguracion()
    {
        var p = await ConCalendarioAsync(c => c.ObtenerParametrosAsync());

        Assert.Equal(new ParametrosBloqueo(180, 60, 120, 720, 60), p);
    }
}
