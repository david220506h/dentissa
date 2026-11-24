using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services.Agenda;
using backend.Services.Correo;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace backend.Tests;

// Fase 2, commit 10 (diseño de la agenda, sección 7): bandeja de salida, plantillas y envío.

/// <summary>Remitente de pruebas: guarda los mensajes y puede fallar las próximas n veces.</summary>
public sealed class EmailSenderEnMemoria : IEmailSender
{
    public ConcurrentQueue<MensajeCorreo> Enviados { get; } = new();
    public int FallarProximos;

    public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
    {
        if (Interlocked.Decrement(ref FallarProximos) >= 0)
            throw new IOException("SMTP no disponible (simulado)");
        Interlocked.Exchange(ref FallarProximos, Math.Max(FallarProximos, 0));
        Enviados.Enqueue(mensaje);
        return Task.CompletedTask;
    }

    public List<MensajeCorreo> Para(string correo) => Enviados.Where(m => m.Para == correo).ToList();
}

public class FabricaCorreo : FabricaDisponibilidad
{
    public EmailSenderEnMemoria Sender { get; } = new();
    protected virtual string UrlPublica => "https://sitio.prueba.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Clinica:Nombre", "Clínica Demo");
        builder.UseSetting("Clinica:Telefono", "555 000 0000");
        builder.UseSetting("Clinica:Direccion", "Calle Ejemplo 123");
        builder.UseSetting("Clinica:UrlPublica", UrlPublica);
        builder.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IEmailSender>(Sender)));
    }

    public Task<int> ProcesarAsync() => Services.GetRequiredService<IProcesadorBandeja>().ProcesarAsync();
}

public class FabricaCorreoSinUrl : FabricaCorreo
{
    protected override string UrlPublica => "";
}

public class CorreoTests : IClassFixture<FabricaCorreo>
{
    private readonly FabricaCorreo _f;
    private static readonly DateTimeOffset Ahora = Local(11, 30, 10, 30);

    public CorreoTests(FabricaCorreo f)
    {
        _f = f;
        _f.Reloj.Fijar(Ahora);
        Interlocked.Exchange(ref _f.Sender.FallarProximos, 0);
    }

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    public sealed record Escenario(int Cita, int Dentista, int Servicio, int Paciente, string? Correo);

    // Dentista con un apellido único para comprobar que nunca sale en el correo
    public static async Task<Escenario> EscenarioAsync(DentissaFactory f, DateTimeOffset inicio, bool conCorreo = true, string estado = EstadosCita.Pendiente) =>
        await f.ConDbAsync(async db =>
        {
            var correo = conCorreo ? $"paciente-{Guid.NewGuid():N}@prueba.test" : null;
            var dentista = new Dentista
            {
                Usuario = new Usuario { Nombre = "Dentista", Apellido = "Ximénez", Correo = $"dent-{Guid.NewGuid():N}@prueba.test", PasswordHash = "x", Rol = Roles.Dentista },
                Especialidad = "General"
            };
            var servicio = new Servicio
            {
                Nombre = "Limpieza dental", Precio = 500, DuracionSlots = 1,
                Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") }
            };
            var paciente = new Paciente { Nombre = "Paciente", Apellido = "Prueba", Correo = correo, Telefono = "5550001111", Origen = OrigenesCita.Recepcion };
            db.AddRange(dentista, servicio, paciente);
            await db.SaveChangesAsync();
            db.DentistaServicios.Add(new DentistaServicio { DentistaId = dentista.Id, ServicioId = servicio.Id });
            var cita = DentissaFactory.NuevaCita(paciente.Id, dentista.Id, servicio.Id, inicio.UtcDateTime);
            cita.Estado = estado;
            db.Citas.Add(cita);
            await db.SaveChangesAsync();
            return new Escenario(cita.Id, dentista.Id, servicio.Id, paciente.Id, correo);
        });

    private Task<List<Notificacion>> FilasAsync(int cita) =>
        _f.ConDbAsync(db => db.Notificaciones.AsNoTracking().Where(n => n.CitaId == cita && n.Canal == CanalesNotificacion.Correo).ToListAsync());

    [Fact]
    public async Task Confirmar_EncolaEnLaTransaccion_YSeEnviaConEnlaceDeGestion()
    {
        var e = await EscenarioAsync(_f, Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        Assert.Equal(HttpStatusCode.OK, (await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null)).StatusCode);

        var fila = Assert.Single(await FilasAsync(e.Cita));
        Assert.Equal(EstadosNotificacion.Pendiente, fila.Estado);
        Assert.Equal(PlantillasCorreo.Confirmacion, fila.Plantilla);
        await _f.ProcesarAsync();

        var m = Assert.Single(_f.Sender.Para(e.Correo!));
        Assert.Equal("Tu cita está confirmada", m.Asunto);
        Assert.Contains("Limpieza dental", m.CuerpoTexto);
        Assert.Contains("martes 1 de diciembre de 2026 a las 09:00", m.CuerpoTexto);
        Assert.Contains("Calle Ejemplo 123", m.CuerpoTexto);
        Assert.DoesNotContain("Ximénez", m.CuerpoTexto + m.CuerpoHtml);
        var enlace = m.CuerpoTexto.Split('\n').Single(l => l.StartsWith("https://sitio.prueba.test/cita#t=")).Trim();
        var token = enlace[(enlace.IndexOf("#t=") + 3)..];
        var guardado = await _f.ConDbAsync(db => db.CitaTokens.AsNoTracking().SingleAsync(t => t.CitaId == e.Cita));
        Assert.Equal(PropositosToken.Gestionar, guardado.Proposito);
        Assert.Equal(TokensCitaService.Hash(token), guardado.TokenHash);
        var enviada = Assert.Single(await FilasAsync(e.Cita));
        Assert.Equal(EstadosNotificacion.Enviada, enviada.Estado);
        Assert.True(enviada.Enviada);
        Assert.DoesNotContain(token, enviada.Mensaje);
    }

    [Fact]
    public async Task Cancelar_SinElMotivo_YReagendar_ConHorarioAnterior_YReasignar_SinCorreo()
    {
        var a = await EscenarioAsync(_f, Local(12, 1, 10));
        var b = await EscenarioAsync(_f, Local(12, 1, 12));
        var c = await EscenarioAsync(_f, Local(12, 1, 13));
        var otro = await EscenarioAsync(_f, Local(12, 3, 9));
        await _f.ConDbAsync(db => { db.DentistaServicios.Add(new DentistaServicio { DentistaId = otro.Dentista, ServicioId = c.Servicio }); return db.SaveChangesAsync(); });
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        await recepcion.PatchAsJsonAsync($"/api/Citas/{a.Cita}/cancelar", new { motivo = "Dolor de muela intenso" });
        await recepcion.PatchAsJsonAsync($"/api/Citas/{b.Cita}/reagendar", new { fecha = "2026-12-02", hora = "15:00:00" });
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PatchAsJsonAsync($"/api/Citas/{c.Cita}/dentista", new { dentistaId = otro.Dentista, motivo = "Agenda" })).StatusCode);
        await _f.ProcesarAsync();

        var cancelacion = Assert.Single(_f.Sender.Para(a.Correo!));
        Assert.Equal("Tu cita fue cancelada", cancelacion.Asunto);
        Assert.DoesNotContain("muela", cancelacion.CuerpoTexto);
        Assert.Contains("llámanos al 555 000 0000", cancelacion.CuerpoTexto);
        var cambio = Assert.Single(_f.Sender.Para(b.Correo!));
        Assert.Contains("Antes: martes 1 de diciembre de 2026 a las 12:00", cambio.CuerpoTexto);
        Assert.Contains("Ahora: miércoles 2 de diciembre de 2026 a las 15:00", cambio.CuerpoTexto);
        Assert.Empty(_f.Sender.Para(c.Correo!));
        Assert.Empty(await FilasAsync(c.Cita));
    }

    [Fact]
    public async Task PacienteSinCorreo_SeDescarta_YAvisaARecepcion()
    {
        var e = await EscenarioAsync(_f, Local(12, 1, 15), conCorreo: false);
        var asistente = await _f.CrearUsuarioAsync(Roles.Asistente);
        var recepcion = await _f.ClienteDeAsync(asistente);
        await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);

        await _f.ProcesarAsync();

        var fila = Assert.Single(await FilasAsync(e.Cita));
        Assert.Equal(EstadosNotificacion.Descartada, fila.Estado);
        Assert.Equal(ProcesadorBandeja.SinCorreo, fila.ErrorEnvio);
        Assert.True(await _f.ConDbAsync(db => db.Notificaciones.AnyAsync(n =>
            n.UsuarioId == asistente.Id && n.CitaId == e.Cita && n.Tipo == TiposNotificacion.EnvioFallido && n.Canal == CanalesNotificacion.Interna)));
    }

    [Fact]
    public async Task Reintentos_ConEsperas_YALaSextaFalla_FallidaConAviso()
    {
        var e = await EscenarioAsync(_f, Local(12, 4, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);
        Interlocked.Exchange(ref _f.Sender.FallarProximos, 1000);

        for (var i = 0; i < ProcesadorBandeja.Esperas.Length; i++)
        {
            await _f.ProcesarAsync();
            var fila = Assert.Single(await FilasAsync(e.Cita));
            Assert.Equal(EstadosNotificacion.Pendiente, fila.Estado);
            Assert.Equal(i + 1, fila.Intentos);
            Assert.Equal(nameof(IOException), fila.ErrorEnvio);
            Assert.Equal(_f.Reloj.GetUtcNow().UtcDateTime + ProcesadorBandeja.Esperas[i], fila.ProximoIntentoEn);
            // Antes de la espera no se reintenta
            await _f.ProcesarAsync();
            Assert.Equal(i + 1, Assert.Single(await FilasAsync(e.Cita)).Intentos);
            _f.Reloj.Avanzar(ProcesadorBandeja.Esperas[i]);
        }
        await _f.ProcesarAsync();

        var final = Assert.Single(await FilasAsync(e.Cita));
        Assert.Equal(EstadosNotificacion.Fallida, final.Estado);
        Assert.Equal(6, final.Intentos);
        Assert.True(await _f.ConDbAsync(db => db.Notificaciones.AnyAsync(n => n.CitaId == e.Cita && n.Tipo == TiposNotificacion.EnvioFallido)));
        Assert.Empty(_f.Sender.Para(e.Correo!));
    }

    [Fact]
    public async Task Arrendamiento_VencidoSeRetoma_VigenteNo_YLasAntiguasSeDescartan()
    {
        var vencido = await EscenarioAsync(_f, Local(12, 7, 9));
        var vigente = await EscenarioAsync(_f, Local(12, 7, 10));
        var antigua = await EscenarioAsync(_f, Local(12, 7, 12));
        var ahora = Ahora.UtcDateTime;
        await _f.ConDbAsync(async db =>
        {
            foreach (var (e, desde, creado) in new[] { (vencido, ahora.AddMinutes(-11), ahora), (vigente, ahora.AddMinutes(-5), ahora), (antigua, (DateTime?)null, ahora.AddHours(-49)) })
            {
                var cita = await db.Citas.SingleAsync(c => c.Id == e.Cita);
                var n = BandejaCorreo.Encolar(db, cita, PlantillasCorreo.Confirmacion, TiposNotificacion.Confirmacion, creado);
                if (desde is not null) { n.Estado = EstadosNotificacion.Procesando; n.ProcesandoDesde = desde; }
            }
            return await db.SaveChangesAsync();
        });

        await _f.ProcesarAsync();

        Assert.Single(_f.Sender.Para(vencido.Correo!));
        Assert.Empty(_f.Sender.Para(vigente.Correo!));
        Assert.Equal(EstadosNotificacion.Procesando, Assert.Single(await FilasAsync(vigente.Cita)).Estado);
        var vieja = Assert.Single(await FilasAsync(antigua.Cita));
        Assert.Equal(EstadosNotificacion.Descartada, vieja.Estado);
        Assert.Equal(ProcesadorBandeja.Antigua, vieja.ErrorEnvio);
    }

    [Fact]
    public async Task DosTrabajadoresALaVez_CadaCorreoUnaSolaVez()
    {
        var escenarios = new List<Escenario>();
        for (var i = 0; i < 6; i++) escenarios.Add(await EscenarioAsync(_f, Local(12, 8, 9)));
        await _f.ConDbAsync(async db =>
        {
            foreach (var e in escenarios)
                BandejaCorreo.Encolar(db, await db.Citas.SingleAsync(c => c.Id == e.Cita), PlantillasCorreo.Confirmacion, TiposNotificacion.Confirmacion, Ahora.UtcDateTime);
            return await db.SaveChangesAsync();
        });

        await Task.WhenAll(Task.Run(_f.ProcesarAsync), Task.Run(_f.ProcesarAsync), Task.Run(_f.ProcesarAsync));

        Assert.All(escenarios, e => Assert.Single(_f.Sender.Para(e.Correo!)));
    }

    [Fact]
    public async Task Vencimiento_AlLiberarseLaSolicitud_SeAvisaAlPaciente()
    {
        _f.Reloj.Fijar(Local(11, 28, 12));
        var vencida = await EscenarioAsync(_f, Local(11, 30, 12));
        await _f.ConDbAsync(db => db.Citas.Where(c => c.Id == vencida.Cita).ExecuteUpdateAsync(s => s
            .SetProperty(c => c.Origen, OrigenesCita.Web).SetProperty(c => c.BloqueoExpiraEn, Local(11, 30, 9).UtcDateTime)));
        _f.Reloj.Fijar(Ahora);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        var nuevo = await _f.ConDbAsync(async db =>
        {
            var p = new Paciente { Nombre = "Otra", Apellido = "Persona", Telefono = "5550002222", Origen = OrigenesCita.Recepcion };
            db.Pacientes.Add(p);
            await db.SaveChangesAsync();
            return p.Id;
        });

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = nuevo, servicioId = vencida.Servicio, dentistaId = vencida.Dentista, fecha = "2026-11-30", hora = "12:00:00" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        await _f.ProcesarAsync();

        var m = Assert.Single(_f.Sender.Para(vencida.Correo!));
        Assert.Equal("Tu solicitud de cita no se confirmó a tiempo", m.Asunto);
        Assert.Contains("lunes 30 de noviembre de 2026 a las 12:00", m.CuerpoTexto);
    }

    [Fact]
    public async Task ReservaRechazada_NoDejaFilasEnLaBandeja()
    {
        var e = await EscenarioAsync(_f, Local(12, 9, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = e.Paciente, servicioId = e.Servicio, dentistaId = e.Dentista, fecha = "2026-12-09", hora = "09:00:00", confirmarAlCrear = true });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.False(await _f.ConDbAsync(db => db.Notificaciones.AnyAsync(n => n.PacienteId == e.Paciente)));
    }
}

public class CorreoSinUrlPublicaTests : IClassFixture<FabricaCorreoSinUrl>
{
    private readonly FabricaCorreoSinUrl _f;

    public CorreoSinUrlPublicaTests(FabricaCorreoSinUrl f)
    {
        _f = f;
        _f.Reloj.Fijar(new DateTimeOffset(2026, 11, 30, 10, 30, 0, TimeSpan.FromHours(-6)));
    }

    [Fact]
    public async Task SinUrlPublica_SinEnlaces_NiTokens()
    {
        var e = await CorreoTests.EscenarioAsync(_f, new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.FromHours(-6)));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);

        await _f.ProcesarAsync();

        var m = Assert.Single(_f.Sender.Para(e.Correo!));
        Assert.DoesNotContain("#t=", m.CuerpoTexto);
        Assert.Contains("Si necesitas cambiarla o cancelarla, llámanos al 555 000 0000.", m.CuerpoTexto);
        Assert.False(await _f.ConDbAsync(db => db.CitaTokens.AnyAsync(t => t.CitaId == e.Cita)));
    }
}

public class PlantillasCorreoTests
{
    private static readonly OpcionesClinica Clinica = new() { Nombre = "Clínica <Demo>", Telefono = "555", Direccion = "Calle 1" };

    [Fact]
    public void TodasLasPlantillas_SeRenderizan_ConHtmlCodificado()
    {
        var datos = new DatosCorreo("Limpieza <script>", new DateOnly(2026, 12, 1), new TimeOnly(9, 0), new DateOnly(2026, 11, 30), new TimeOnly(12, 0),
            "https://sitio.test/cita#t=abc", "https://sitio.test/cita#t=def");
        foreach (var p in PlantillasCorreo.Todas)
        {
            var (asunto, texto, html) = PlantillasCorreoRender.Renderizar(p, datos, Clinica);
            Assert.False(string.IsNullOrWhiteSpace(asunto));
            Assert.StartsWith("Hola:", texto);
            Assert.DoesNotContain("<script>", html);
            Assert.DoesNotContain("<Demo>", html);
            Assert.Contains("Este correo se envió automáticamente", texto);
        }
    }

    [Fact]
    public void SolicitudRecibida_ConYSinEnlaceDeConfirmar()
    {
        var con = PlantillasCorreoRender.Renderizar(PlantillasCorreo.SolicitudRecibida,
            new DatosCorreo("Limpieza", new DateOnly(2026, 12, 1), new TimeOnly(9, 0), EnlaceConfirmar: "https://s/cita#t=x"), Clinica);
        var sin = PlantillasCorreoRender.Renderizar(PlantillasCorreo.SolicitudRecibida,
            new DatosCorreo("Limpieza", new DateOnly(2026, 12, 1), new TimeOnly(9, 0)), Clinica);

        Assert.Contains("https://s/cita#t=x", con.Texto);
        Assert.Contains("Tu cita aún no está confirmada.", con.Texto);
        Assert.Contains("Te llamaremos para confirmarla.", sin.Texto);
    }
}

// Migración de notificaciones: las filas de correo anteriores reciben estado y plantilla
public class BandejaCorreoMigracionTests
{
    [Fact]
    public async Task FilasViejas_NoEnviadasQuedanDescartadas_YEnviadasEnviadas()
    {
        var cadena = await PostgresCompartido.CrearBaseVaciaAsync();
        await using (var db = new DentissaDbContext(new DbContextOptionsBuilder<DentissaDbContext>().UseNpgsql(cadena).UseSnakeCaseNamingConvention().Options))
        {
            var migraciones = db.Database.GetMigrations().ToList();
            var anterior = migraciones[migraciones.FindIndex(m => m.EndsWith("_BandejaCorreo")) - 1];
            await db.GetService<IMigrator>().MigrateAsync(anterior);
            await db.Database.ExecuteSqlRawAsync("""
                WITH p AS (INSERT INTO pacientes (nombre, apellido, telefono, origen, creado_en, actualizado_en)
                           VALUES ('Paciente', 'Prueba', '5550001111', 'recepcion', now(), now()) RETURNING id)
                INSERT INTO notificaciones (paciente_id, tipo, canal, mensaje, enviada, leida, creado_en)
                SELECT id, 'confirmacion', 'correo', 'viejo', false, false, now() FROM p
                UNION ALL SELECT id, 'recordatorio', 'correo', 'viejo', true, false, now() FROM p
                """);
            await db.Database.MigrateAsync();
        }

        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT tipo, estado, plantilla FROM notificaciones ORDER BY tipo", conexion);
        await using var r = await comando.ExecuteReaderAsync();
        var filas = new List<(string, string, string)>();
        while (await r.ReadAsync()) filas.Add((r.GetString(0), r.GetString(1), r.GetString(2)));
        Assert.Equal([("confirmacion", "descartada", "confirmacion"), ("recordatorio", "enviada", "recordatorio")], filas);
    }
}

// Integración con Mailpit real (Testcontainers): el remitente SMTP de MailKit entrega el mensaje
public class MailpitTests : IAsyncLifetime
{
    private readonly DotNet.Testcontainers.Containers.IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.33.1")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/info")))
        .Build();

    public Task InitializeAsync() => _mailpit.StartAsync();

    public Task DisposeAsync() => _mailpit.DisposeAsync().AsTask();

    [Fact]
    public async Task SmtpEmailSender_EntregaEnMailpit()
    {
        var opciones = new OpcionesCorreo
        {
            Habilitado = true, Remitente = "citas@prueba.test", NombreRemitente = "Dentissa pruebas",
            Smtp = new OpcionesSmtp { Host = _mailpit.Hostname, Puerto = _mailpit.GetMappedPublicPort(1025), Seguridad = "Ninguna" }
        };
        var sender = new SmtpEmailSender(new OpcionesFijas<OpcionesCorreo>(opciones));
        var (asunto, texto, html) = PlantillasCorreoRender.Renderizar(PlantillasCorreo.Confirmacion,
            new DatosCorreo("Limpieza dental", new DateOnly(2026, 12, 1), new TimeOnly(9, 0)), new OpcionesClinica { Nombre = "Clínica Demo" });

        await sender.EnviarAsync(new MensajeCorreo("paciente@prueba.test", asunto, texto, html));

        using var http = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(8025)}") };
        var lista = await http.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
        var mensaje = lista.GetProperty("messages")[0];
        Assert.Equal("Tu cita está confirmada", mensaje.GetProperty("Subject").GetString());
        Assert.Equal("paciente@prueba.test", mensaje.GetProperty("To")[0].GetProperty("Address").GetString());
        Assert.Equal("citas@prueba.test", mensaje.GetProperty("From").GetProperty("Address").GetString());
    }

    private sealed class OpcionesFijas<T>(T valor) : IOptionsMonitor<T>
    {
        public T CurrentValue => valor;
        public T Get(string? name) => valor;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
