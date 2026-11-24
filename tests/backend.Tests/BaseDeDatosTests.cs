using System.Net;
using System.Net.Http.Json;
using backend.Constants;
using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace backend.Tests;

// Pruebas que solo tienen sentido contra PostgreSQL real: migración, restricciones,
// triggers y concurrencia.
public class MigracionTests
{
    [Fact]
    public async Task Migracion_SeAplicaDesdeUnaBaseVacia_SinCambiosPendientes()
    {
        using var factory = new DentissaFactoryBaseVacia();
        _ = factory.Services; // arranca la API: aplica la migración y crea los datos iniciales

        await factory.ConDbAsync(async db =>
        {
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            // Todas las migraciones del proyecto quedan aplicadas, en orden
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.True(await db.ConfiguracionAgenda.AnyAsync());
            return true;
        });
    }
}

public class RestriccionesTests : IClassFixture<DentissaFactory>
{
    private static readonly DateTime Lunes9 = new(2031, 1, 6, 15, 0, 0, DateTimeKind.Utc); // 9:00 en la clínica

    private readonly DentissaFactory _factory;

    public RestriccionesTests(DentissaFactory factory) => _factory = factory;

    private async Task<PostgresException?> GuardarAsync(Action<DentissaDbContext> cambios)
    {
        try
        {
            await _factory.ConDbAsync(async db =>
            {
                cambios(db);
                return await db.SaveChangesAsync();
            });
            return null;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            return pg;
        }
    }

    [Fact]
    public async Task SinTraslape_ComparaRangosYSoloCitasActivas()
    {
        var (paciente, dentistaA, servicio) = await _factory.CrearParticipantesAsync(2);
        var (_, dentistaB, _) = await _factory.CrearParticipantesAsync();

        // 9:00–11:00 (dos slots)
        var larga = DentissaFactory.NuevaCita(paciente, dentistaA, servicio, Lunes9, horas: 2);
        Assert.Null(await GuardarAsync(db => db.Citas.Add(larga)));

        // 10:00–11:00 del mismo dentista: choca con la de dos slots
        var choque = await GuardarAsync(db => db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentistaA, servicio, Lunes9.AddHours(1))));
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, choque?.SqlState);
        Assert.Equal("citas_sin_traslape_dentista", choque?.ConstraintName);

        // Mismo horario con otro dentista: permitido
        Assert.Null(await GuardarAsync(db => db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentistaB, servicio, Lunes9.AddHours(1)))));

        // Contigua (empieza cuando termina la otra, rango [)): permitida
        Assert.Null(await GuardarAsync(db => db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentistaA, servicio, Lunes9.AddHours(2)))));

        // Sobre una cita cancelada: permitida
        await _factory.ConDbAsync(async db =>
        {
            var c = await db.Citas.SingleAsync(x => x.Id == larga.Id);
            c.Estado = EstadosCita.Cancelada;
            c.MotivoCancelacion = "Prueba";
            return await db.SaveChangesAsync();
        });
        Assert.Null(await GuardarAsync(db => db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentistaA, servicio, Lunes9))));
    }

    [Fact]
    public async Task Check_RolEstadoMotivoYBloqueo()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();
        var inicio = Lunes9.AddDays(7);

        var rol = await GuardarAsync(db => db.Usuarios.Add(new Usuario
        {
            Nombre = "X", Apellido = "Y", Correo = $"rol-{Guid.NewGuid():N}@prueba.test", PasswordHash = "h", Rol = "secretaria"
        }));
        Assert.Equal("ck_usuarios_rol", rol?.ConstraintName);

        var estado = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio);
        estado.Estado = "borrador";
        Assert.Equal("ck_citas_estado", (await GuardarAsync(db => db.Citas.Add(estado)))?.ConstraintName);

        var sinMotivo = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio.AddHours(1));
        sinMotivo.Estado = EstadosCita.Cancelada;
        Assert.Equal("ck_citas_cancelacion_con_motivo", (await GuardarAsync(db => db.Citas.Add(sinMotivo)))?.ConstraintName);

        var bloqueo = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio.AddHours(2));
        bloqueo.BloqueoExpiraEn = DateTime.UtcNow.AddHours(24); // origen recepcion
        Assert.Equal("ck_citas_bloqueo_solo_solicitudes", (await GuardarAsync(db => db.Citas.Add(bloqueo)))?.ConstraintName);
    }

    [Fact]
    public async Task FichaDeDentista_SoloConCuentaDeRolDentista()
    {
        var asistente = await _factory.CrearUsuarioAsync(Roles.Asistente);

        var error = await GuardarAsync(db => db.Dentistas.Add(new Dentista { UsuarioId = asistente.Id }));

        Assert.Equal("fk_dentistas_usuarios_id_rol", error?.ConstraintName);
    }

    [Fact]
    public async Task Correo_UnicoSinMayusculas_YLaCuentaEliminadaLoLibera()
    {
        var correo = $"Unico-{Guid.NewGuid():N}@Prueba.test";
        var original = await _factory.CrearUsuarioAsync(Roles.Asistente, correo);

        var duplicado = await GuardarAsync(db => db.Usuarios.Add(new Usuario
        {
            Nombre = "X", Apellido = "Y", Correo = correo.ToLowerInvariant(), PasswordHash = "h", Rol = Roles.Asistente
        }));
        Assert.Equal("ux_usuarios_correo_vigente", duplicado?.ConstraintName);

        // El login encuentra la cuenta sin importar mayúsculas (citext)
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = correo.ToUpperInvariant(), password = DentissaFactory.PasswordPruebas });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        await _factory.ConDbAsync(async db =>
        {
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Id == original.Id));
            return await db.SaveChangesAsync();
        });
        Assert.Null(await GuardarAsync(db => db.Usuarios.Add(new Usuario
        {
            Nombre = "X", Apellido = "Y", Correo = correo.ToLowerInvariant(), PasswordHash = "h", Rol = Roles.Asistente
        })));
    }

    [Fact]
    public async Task Oferta_VigenciaInvertidaRechazada_YAplicaElPrimerYUltimoDia()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();

        // Semana laborable de dentro de dos a tres semanas: el panel respeta el horizonte de 60 días
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var lunes = hoy.AddDays(14 + ((8 - (int)hoy.DayOfWeek) % 7));
        var sabado = lunes.AddDays(5);
        var invertida = await GuardarAsync(db => db.Ofertas.Add(new Oferta
        {
            ServicioId = servicio, Descuento = 10, FechaInicio = lunes.AddDays(6), FechaFin = lunes
        }));
        Assert.Equal("ck_ofertas_vigencia", invertida?.ConstraintName);

        // Lunes a sábado (fechas locales, inclusivas)
        Assert.Null(await GuardarAsync(db => db.Ofertas.Add(new Oferta
        {
            ServicioId = servicio, Descuento = 10, FechaInicio = lunes, FechaFin = sabado
        })));

        var client = await _factory.ClienteConRolAsync(Roles.Asistente);
        async Task<decimal> DescuentoAsync(string fecha, string hora)
        {
            var res = await client.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha, hora });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var id = (await res.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["id"].ToString();
            var cita = await client.GetFromJsonAsync<Dictionary<string, object>>($"/api/Citas/{id}");
            return decimal.Parse(cita!["descuentoAplicado"].ToString()!, System.Globalization.CultureInfo.InvariantCulture);
        }

        Assert.Equal(10m, await DescuentoAsync(lunes.ToString("yyyy-MM-dd"), "08:00:00"));  // primer día, primer horario
        Assert.Equal(10m, await DescuentoAsync(sabado.ToString("yyyy-MM-dd"), "15:00:00"));  // último día, último horario
        Assert.Equal(0m, await DescuentoAsync(lunes.AddDays(7).ToString("yyyy-MM-dd"), "08:00:00"));   // lunes siguiente
    }

    [Theory]
    [InlineData("UPDATE auditoria SET accion = 'x'")]
    [InlineData("DELETE FROM auditoria")]
    [InlineData("TRUNCATE auditoria")]
    [InlineData("UPDATE cita_historial SET detalle = 'x'")]
    [InlineData("DELETE FROM cita_historial")]
    [InlineData("TRUNCATE cita_historial")]
    [InlineData("DELETE FROM examen_medico")]
    [InlineData("TRUNCATE consulta_fotos")]
    [InlineData("DELETE FROM citas")]
    [InlineData("TRUNCATE citas CASCADE")]
    public async Task BitacoraExpedienteYCitas_RechazanModificacionYBorrado(string sql)
    {
        // Garantiza al menos una fila en la bitácora y en el historial
        await _factory.CrearCitaAsync();
        var citaId = await _factory.CrearCitaAsync();
        var asistente = await _factory.ClienteConRolAsync(Roles.Asistente);
        await asistente.PatchAsync($"/api/Citas/{citaId}/confirmar", null);
        await _factory.ConDbAsync(async db =>
        {
            var pacienteId = await db.Citas.Where(c => c.Id == citaId).Select(c => c.PacienteId).SingleAsync();
            db.ExamenesMedicos.Add(new ExamenMedico { PacienteId = pacienteId, MotivoConsulta = "Revisión" });
            return await db.SaveChangesAsync();
        });

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            _factory.ConDbAsync(db => db.Database.ExecuteSqlRawAsync(sql)));

        Assert.Equal(PostgresErrorCodes.RestrictViolation, ex.SqlState);
    }
}

// Dos inserciones simultáneas del mismo horario y dentista, con contextos y conexiones
// distintos y una barrera para que lleguen juntas a la base: exactamente una se guarda.
public class ConcurrenciaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public ConcurrenciaTests(DentissaFactory factory) => _factory = factory;

    [Fact]
    public async Task DirectoContraLaBase_UnaSeGuardaYLaOtraRecibe23P01()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();
        var inicio = new DateTime(2031, 2, 3, 16, 0, 0, DateTimeKind.Utc); // lunes 10:00 local
        using var barrera = new Barrier(2);

        async Task<Exception?> InsertarAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DentissaDbContext>();
            db.Citas.Add(DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio));
            await db.Database.OpenConnectionAsync(); // conexión propia abierta antes de la barrera
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            try
            {
                await db.SaveChangesAsync();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var resultados = await Task.WhenAll(Task.Run(InsertarAsync), Task.Run(InsertarAsync));

        Assert.Single(resultados, r => r == null);
        var error = Assert.Single(resultados, r => r != null);
        // Sin reintento, la base rechaza la segunda con 23P01 (exclusión) o, si ambas
        // esperaban una a la otra dentro de la restricción, con 40P01 (interbloqueo).
        // La API reintenta el 40P01 y siempre responde 409 (prueba siguiente).
        var pg = ErrorDePostgres(error);
        Assert.Contains(pg?.SqlState, new[] { PostgresErrorCodes.ExclusionViolation, PostgresErrorCodes.DeadlockDetected });
        Assert.Equal(1, await _factory.ConDbAsync(db => db.Citas.CountAsync(c => c.DentistaId == dentista)));
    }

    private static PostgresException? ErrorDePostgres(Exception? ex)
    {
        for (; ex != null; ex = ex.InnerException)
            if (ex is PostgresException pg) return pg;
        return null;
    }

    [Fact]
    public async Task PorLaApi_UnaRecibe201YLaOtra409()
    {
        var (paciente, dentista, servicio) = await _factory.CrearParticipantesAsync();
        var (otroPaciente, _, _) = await _factory.CrearParticipantesAsync();
        var recepcion1 = await _factory.ClienteConRolAsync(Roles.Asistente);
        var recepcion2 = await _factory.ClienteConRolAsync(Roles.Asistente);
        using var barrera = new Barrier(2);

        async Task<HttpResponseMessage> AgendarAsync(HttpClient client, int pacienteId)
        {
            barrera.SignalAndWait(TimeSpan.FromSeconds(30));
            return await client.PostAsJsonAsync("/api/Citas", new
            {
                pacienteId, dentistaId = dentista, servicioId = servicio, fecha = ConcurrenciaReservasTests.FechaLaborable(), hora = "12:00:00"
            });
        }

        var respuestas = await Task.WhenAll(
            Task.Run(() => AgendarAsync(recepcion1, paciente)),
            Task.Run(() => AgendarAsync(recepcion2, otroPaciente)));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        var rechazada = Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        var cuerpo = await rechazada.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("El dentista ya tiene una cita en ese horario.", cuerpo!["mensaje"]);
        Assert.Equal(1, await _factory.ConDbAsync(db => db.Citas.CountAsync(c => c.DentistaId == dentista)));
    }
}
