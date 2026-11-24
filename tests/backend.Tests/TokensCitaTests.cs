using backend.Entities;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace backend.Tests;

// Fase 2, commit 9 (diseño de la agenda, sección 5): tokens de los enlaces del correo.
public class TokensCitaTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    private static readonly DateTimeOffset Ahora = Local(11, 30, 10);

    public TokensCitaTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(Ahora);
    }

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private async Task<T> ConTokensAsync<T>(Func<ITokensCitaService, DentissaDbContext, Task<T>> accion)
    {
        using var scope = _f.Services.CreateScope();
        return await accion(scope.ServiceProvider.GetRequiredService<ITokensCitaService>(), scope.ServiceProvider.GetRequiredService<DentissaDbContext>());
    }

    // Cita pendiente en línea del martes 1-dic a las 12:00 con bloqueo hasta las 9:00 de ese día
    private async Task<int> CitaAsync(DateTimeOffset? bloqueo = null, string estado = EstadosCita.Pendiente)
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        return await _f.ConDbAsync(async db =>
        {
            var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, Local(12, 1, 12).UtcDateTime);
            c.Origen = OrigenesCita.Web;
            c.Estado = estado;
            c.BloqueoExpiraEn = (bloqueo ?? Local(12, 1, 9)).UtcDateTime;
            db.Citas.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        });
    }

    private Task<string> EmitirAsync(int cita, string proposito = PropositosToken.Confirmar) =>
        ConTokensAsync((t, db) => t.EmitirAsync(db, cita, proposito));

    private Task<int?> ConsumirAsync(string? token, string proposito = PropositosToken.Confirmar) =>
        ConTokensAsync((t, db) => t.ConsumirAsync(db, token, proposito));

    [Fact]
    public async Task Emitir_SoloGuardaElHash_YElTokenEsBase64UrlDe32Bytes()
    {
        var cita = await CitaAsync();

        var token = await EmitirAsync(cita);

        Assert.Equal(43, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", token);
        var fila = await _f.ConDbAsync(db => db.CitaTokens.AsNoTracking().SingleAsync(t => t.CitaId == cita));
        Assert.Equal(TokensCitaService.Hash(token), fila.TokenHash);
        Assert.Matches("^[0-9a-f]{64}$", fila.TokenHash);
        // El token en claro no aparece en ninguna columna de texto de la tabla
        var apariciones = await _f.ConDbAsync(db => db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM cita_tokens t WHERE t::text LIKE {"%" + token + "%"}").SingleAsync());
        Assert.Equal(0, apariciones);
    }

    [Fact]
    public async Task Consumir_Valido_DespuesReuso_Nulo()
    {
        var cita = await CitaAsync();
        var token = await EmitirAsync(cita);

        Assert.Equal(cita, await ConsumirAsync(token));
        Assert.Null(await ConsumirAsync(token));
        Assert.NotNull((await _f.ConDbAsync(db => db.CitaTokens.AsNoTracking().SingleAsync(t => t.CitaId == cita))).UsadoEn);
    }

    [Fact]
    public async Task Vencido_Alterado_PropositoEquivocado_YVacio_SonNulos()
    {
        var cita = await CitaAsync();
        var token = await EmitirAsync(cita);
        var alterado = (token[0] == 'A' ? 'B' : 'A') + token[1..];

        Assert.Null(await ConsumirAsync(alterado));
        Assert.Null(await ConsumirAsync(token, PropositosToken.Gestionar));
        Assert.Null(await ConsumirAsync(""));
        Assert.Null(await ConsumirAsync(null));
        Assert.Null(await ConsumirAsync(new string('a', 500)));
        _f.Reloj.Fijar(Local(12, 1, 9)); // vence con el bloqueo
        Assert.Null(await ConsumirAsync(token));
    }

    [Fact]
    public async Task Reenvio_InvalidaElAnteriorDelMismoProposito()
    {
        var cita = await CitaAsync();
        var primero = await EmitirAsync(cita);
        var gestionar = await EmitirAsync(cita, PropositosToken.Gestionar);
        var segundo = await EmitirAsync(cita);

        Assert.Null(await ConsumirAsync(primero));
        Assert.Equal(cita, await ConsumirAsync(segundo));
        Assert.Equal(cita, await ConsumirAsync(gestionar, PropositosToken.Gestionar));
    }

    [Fact]
    public async Task Vencimiento_ConfirmarConElBloqueo_GestionarConElInicio()
    {
        var cita = await CitaAsync();
        await EmitirAsync(cita);
        await EmitirAsync(cita, PropositosToken.Gestionar);

        var filas = await _f.ConDbAsync(db => db.CitaTokens.AsNoTracking().Where(t => t.CitaId == cita).ToListAsync());
        Assert.Equal(Local(12, 1, 9).UtcDateTime, filas.Single(t => t.Proposito == PropositosToken.Confirmar).ExpiraEn);
        Assert.Equal(Local(12, 1, 12).UtcDateTime, filas.Single(t => t.Proposito == PropositosToken.Gestionar).ExpiraEn);
    }

    [Fact]
    public async Task NoSeEmite_SiLaCitaYaNoLoAdmite()
    {
        var confirmada = await CitaAsync(estado: EstadosCita.Confirmada);
        _f.Reloj.Fijar(Local(11, 28, 12));
        var vencida = await CitaAsync(Local(11, 30, 9, 30)); // el bloqueo ya pasó ("ahora" 10:00)
        _f.Reloj.Fijar(Ahora);

        await Assert.ThrowsAsync<EnlaceNoDisponibleException>(() => EmitirAsync(confirmada));
        await Assert.ThrowsAsync<EnlaceNoDisponibleException>(() => EmitirAsync(vencida));
        Assert.NotNull(await EmitirAsync(confirmada, PropositosToken.Gestionar));
    }

    [Fact]
    public async Task DosConsumosSimultaneos_SoloUnoGana()
    {
        var cita = await CitaAsync();
        var token = await EmitirAsync(cita);
        using var barrera = new Barrier(2);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            return await ConsumirAsync(token);
        })));

        Assert.Single(resultados, r => r == cita);
        Assert.Single(resultados, r => r == null);
    }

    [Theory]
    [InlineData("DELETE FROM cita_tokens")]
    [InlineData("TRUNCATE cita_tokens")]
    public async Task SinBorradoFisico(string sql)
    {
        await EmitirAsync(await CitaAsync());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => _f.ConDbAsync(db => db.Database.ExecuteSqlRawAsync(sql)));

        Assert.Equal(PostgresErrorCodes.RestrictViolation, ex.SqlState);
    }

    [Fact]
    public async Task Check_PropositoDesconocido_Rechazado()
    {
        var cita = await CitaAsync();

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _f.ConDbAsync(db =>
        {
            db.CitaTokens.Add(new CitaToken { CitaId = cita, Proposito = "otro", TokenHash = new string('a', 64), ExpiraEn = DateTime.UtcNow.AddDays(1), CreadoEn = DateTime.UtcNow });
            return db.SaveChangesAsync();
        }));

        Assert.Equal("ck_cita_tokens_proposito", (ex.InnerException as PostgresException)?.ConstraintName);
    }
}
