using backend.Entities;
using backend.Services.Correo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests;

// Commit 16: recordatorio 24 h antes, solo de citas confirmadas y una sola vez
public class RecordatoriosTests : IClassFixture<FabricaCorreo>
{
    private readonly FabricaCorreo _f;
    private static readonly DateTimeOffset Ahora = new(2026, 11, 30, 10, 0, 0, TimeSpan.FromHours(-6));

    public RecordatoriosTests(FabricaCorreo f)
    {
        _f = f;
        _f.Reloj.Fijar(Ahora);
    }

    private async Task<int> CrearAsync()
    {
        using var scope = _f.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IRecordatorios>().CrearAsync();
    }

    [Fact]
    public async Task SoloConfirmadasEnLaVentana_UnaVez_YSeEnvian()
    {
        var manana = await CorreoTests.EscenarioAsync(_f, Ahora.AddHours(24), estado: EstadosCita.Confirmada);
        var pendiente = await CorreoTests.EscenarioAsync(_f, Ahora.AddHours(24).AddMinutes(10));
        var lejana = await CorreoTests.EscenarioAsync(_f, Ahora.AddHours(30), estado: EstadosCita.Confirmada);

        await CrearAsync();
        await CrearAsync(); // repetir no duplica

        var filas = await _f.ConDbAsync(db => db.Notificaciones.AsNoTracking().Where(n => n.Tipo == TiposNotificacion.Recordatorio).ToListAsync());
        Assert.Single(filas, n => n.CitaId == manana.Cita);
        Assert.DoesNotContain(filas, n => n.CitaId == pendiente.Cita || n.CitaId == lejana.Cita);

        await _f.ProcesarAsync();
        var m = Assert.Single(_f.Sender.Para(manana.Correo!));
        Assert.Equal("Recordatorio: tu cita es mañana", m.Asunto);
        Assert.Contains("#t=", m.CuerpoTexto); // enlace de gestión
    }
}
