using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace backend.Tests;

// Fase 2, commit 2 (diseño de la agenda, 1.6): configuración en minutos de atención
public class ConfiguracionAgendaMigracionTests
{
    private const string Inicial = "20261120005504_Inicial";

    private static DentissaDbContext Contexto(string cadena) => new(
        new DbContextOptionsBuilder<DentissaDbContext>().UseNpgsql(cadena).UseSnakeCaseNamingConvention().Options);

    private static async Task<(int Anticipacion, int Vida, int Liberacion, int Recepcion, int Horizonte)> LeerAsync(string cadena)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT anticipacion_en_linea_atencion_minutos, vida_maxima_solicitud_atencion_minutos, " +
            "liberacion_antes_cita_atencion_minutos, anticipacion_recepcion_minutos, horizonte_reserva_dias " +
            "FROM configuracion_agenda WHERE id = 1", conexion);
        await using var r = await comando.ExecuteReaderAsync();
        Assert.True(await r.ReadAsync());
        return (r.GetInt16(0), r.GetInt16(1), r.GetInt16(2), r.GetInt16(3), r.GetInt16(4));
    }

    // Base con la migración Inicial, la configuración de la Fase 1 (en horas) y luego todas las migraciones
    private static async Task<string> BaseConConfiguracionViejaAsync(int anticipacionHoras, int bloqueoHoras, int liberacionHoras)
    {
        var cadena = await PostgresCompartido.CrearBaseVaciaAsync();
        await using (var db = Contexto(cadena))
        {
            await db.GetService<IMigrator>().MigrateAsync(Inicial);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO configuracion_agenda (id, duracion_slot_minutos, anticipacion_minima_horas, " +
                "bloqueo_pendiente_horas, liberacion_antes_cita_horas, actualizado_en) VALUES (1, 60, {0}, {1}, {2}, now())",
                anticipacionHoras, bloqueoHoras, liberacionHoras);
            await db.Database.MigrateAsync();
        }
        return cadena;
    }

    [Fact]
    public async Task FilaSembrada_PasaALosValoresAprobados()
    {
        // Valores de siembra de la Fase 1: 2 h, 24 h y 2 h
        var cadena = await BaseConConfiguracionViejaAsync(2, 24, 2);

        Assert.Equal((180, 720, 120, 60, 60), await LeerAsync(cadena));
    }

    [Fact]
    public async Task ConfiguracionPersonalizada_SoloSeConvierte()
    {
        var cadena = await BaseConConfiguracionViejaAsync(4, 10, 1);

        Assert.Equal((240, 600, 60, 60, 60), await LeerAsync(cadena));
    }

    [Fact]
    public async Task ConfiguracionQueNoCumpliria_SeCorrigeLoMinimo()
    {
        // Anticipación 1 h = liberación 1 h: la restricción nueva exige anticipación > liberación
        var cadena = await BaseConConfiguracionViejaAsync(1, 24, 1);

        Assert.Equal((120, 1440, 60, 60, 60), await LeerAsync(cadena));
    }

    [Fact]
    public async Task Down_RegresaAHoras()
    {
        var cadena = await BaseConConfiguracionViejaAsync(2, 24, 2);
        await using (var db = Contexto(cadena))
            await db.GetService<IMigrator>().MigrateAsync(Inicial);

        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT anticipacion_minima_horas, bloqueo_pendiente_horas, liberacion_antes_cita_horas FROM configuracion_agenda", conexion);
        await using var r = await comando.ExecuteReaderAsync();
        Assert.True(await r.ReadAsync());
        // 180, 720 y 120 minutos → 3, 12 y 2 horas
        Assert.Equal((3, 12, 2), ((int)r.GetInt16(0), (int)r.GetInt16(1), (int)r.GetInt16(2)));
    }
}

public class ConfiguracionAgendaTests : IClassFixture<ConfiguracionAgendaTests.FabricaConReloj>
{
    public static readonly DateTimeOffset Ahora = new(2026, 11, 30, 15, 0, 0, TimeSpan.Zero);

    public class FabricaConReloj : DentissaFactory
    {
        public override TimeProvider? Tiempo { get; } = new RelojFijo(Ahora);
    }

    private readonly FabricaConReloj _factory;

    public ConfiguracionAgendaTests(FabricaConReloj factory) => _factory = factory;

    [Fact]
    public async Task DatosIniciales_UsanLosValoresAprobados()
    {
        var config = await _factory.ConDbAsync(db => db.ConfiguracionAgenda.AsNoTracking().SingleAsync());

        Assert.Equal(180, config.AnticipacionEnLineaAtencionMinutos);
        Assert.Equal(60, config.AnticipacionRecepcionMinutos);
        Assert.Equal(120, config.LiberacionAntesCitaAtencionMinutos);
        Assert.Equal(720, config.VidaMaximaSolicitudAtencionMinutos);
        Assert.Equal(60, config.HorizonteReservaDias);
    }

    [Fact]
    public async Task Check_AnticipacionDebeSerMayorQueLiberacion()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => _factory.ConDbAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE configuracion_agenda SET anticipacion_en_linea_atencion_minutos = liberacion_antes_cita_atencion_minutos")));

        Assert.Equal("ck_configuracion_agenda_anticipacion_mayor_que_liberacion", ex.ConstraintName);
    }

    [Theory]
    [InlineData("vida_maxima_solicitud_atencion_minutos = 0")]
    [InlineData("horizonte_reserva_dias = 0")]
    [InlineData("anticipacion_recepcion_minutos = -1")]
    public async Task Check_ValoresPositivos(string asignacion)
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => _factory.ConDbAsync(db =>
            db.Database.ExecuteSqlRawAsync($"UPDATE configuracion_agenda SET {asignacion}")));

        Assert.Equal("ck_configuracion_agenda_valores", ex.ConstraintName);
    }

    [Fact]
    public async Task CreadoEn_SaleDelTimeProvider()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();
        var cita = DentissaFactory.NuevaCita(paciente, dentista, servicio, Ahora.UtcDateTime.AddDays(3));
        await _factory.ConDbAsync(db => { db.Citas.Add(cita); return db.SaveChangesAsync(); });

        var guardada = await _factory.ConDbAsync(db => db.Citas.AsNoTracking().SingleAsync(c => c.Id == cita.Id));
        Assert.Equal(Ahora.UtcDateTime, guardada.CreadoEn);
    }

    [Fact]
    public async Task Check_BloqueoDebeVencerDespuesDeCrearse()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();
        var cita = DentissaFactory.NuevaCita(paciente, dentista, servicio, Ahora.UtcDateTime.AddDays(4));
        cita.Origen = OrigenesCita.Web;
        cita.BloqueoExpiraEn = Ahora.UtcDateTime; // igual a creado_en: no vale

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _factory.ConDbAsync(db => { db.Citas.Add(cita); return db.SaveChangesAsync(); }));

        Assert.Equal("ck_citas_bloqueo_despues_de_creacion", (ex.InnerException as PostgresException)?.ConstraintName);
    }
}
