using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services;
using backend.Services.Agenda;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Tests;

// Fase 2, commit 7 (diseño de la agenda, sección 4): transiciones, historial genérico y concurrencia optimista.
public class TransicionesCitaPurasTests
{
    private static readonly DateTime Inicio = new(2026, 11, 30, 16, 0, 0, DateTimeKind.Utc);

    private static Cita En(string estado) => new() { Estado = estado, Inicio = Inicio, Fin = Inicio.AddHours(1) };

    [Theory]
    [InlineData(EstadosCita.Pendiente, true)]
    [InlineData(EstadosCita.Confirmada, true)]
    [InlineData(EstadosCita.Completada, false)]
    [InlineData(EstadosCita.NoAsistio, false)]
    [InlineData(EstadosCita.Cancelada, false)]
    public void Cancelar_SoloActivas(string estado, bool valida)
    {
        var accion = () => TransicionesCita.ValidarCancelar(En(estado));
        if (valida) accion(); else Assert.Throws<TransicionInvalidaException>(accion);
    }

    [Fact]
    public void Completar_DesdeElInicio_NoAsistio_DesdeInicioMas15()
    {
        Assert.Throws<TransicionInvalidaException>(() => TransicionesCita.ValidarCompletar(En(EstadosCita.Pendiente), Inicio.AddMinutes(-1)));
        TransicionesCita.ValidarCompletar(En(EstadosCita.Pendiente), Inicio);
        Assert.Throws<TransicionInvalidaException>(() => TransicionesCita.ValidarNoAsistio(En(EstadosCita.Confirmada), Inicio.AddMinutes(14)));
        TransicionesCita.ValidarNoAsistio(En(EstadosCita.Confirmada), Inicio.AddMinutes(15));
    }

    [Theory]
    [InlineData(EstadosCita.Completada, EstadosCita.NoAsistio, 0, true)]
    [InlineData(EstadosCita.NoAsistio, EstadosCita.Completada, 7, true)]
    [InlineData(EstadosCita.Completada, EstadosCita.NoAsistio, 8, false)]
    [InlineData(EstadosCita.Completada, EstadosCita.Completada, 0, false)]
    [InlineData(EstadosCita.Cancelada, EstadosCita.Completada, 0, false)]
    [InlineData(EstadosCita.Pendiente, EstadosCita.Completada, 0, false)]
    [InlineData(EstadosCita.Completada, EstadosCita.Pendiente, 0, false)]
    public void Correccion_SoloCompletadaYNoAsistio_HastaSieteDias(string desde, string hacia, int dias, bool valida)
    {
        var accion = () => TransicionesCita.ValidarCorreccion(En(desde), hacia, Inicio.AddDays(dias));
        if (valida) accion(); else Assert.Throws<TransicionInvalidaException>(accion);
    }

    [Fact]
    public void Confirmar_IdempotenteSiYaEstaConfirmada()
    {
        Assert.True(TransicionesCita.ValidarConfirmar(En(EstadosCita.Pendiente)));
        Assert.False(TransicionesCita.ValidarConfirmar(En(EstadosCita.Confirmada)));
        Assert.Throws<TransicionInvalidaException>(() => TransicionesCita.ValidarConfirmar(En(EstadosCita.Cancelada)));
    }
}

public class TransicionesTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    // "Ahora": lunes 30-nov-2026, 10:30 en la clínica
    private static readonly DateTimeOffset Ahora = Local(11, 30, 10, 30);

    public TransicionesTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(Ahora);
    }

    private static DateTimeOffset Local(int mes, int dia, int hora, int minuto = 0) =>
        new(2026, mes, dia, hora, minuto, 0, TimeSpan.FromHours(-6));

    private sealed record Escenario(int Cita, int Dentista, int Servicio, int Paciente, Usuario CuentaDentista);

    // Dentista con cuenta real (puede iniciar sesión), servicio ligado y una cita en el estado pedido
    private async Task<Escenario> CitaAsync(DateTimeOffset inicio, string estado = EstadosCita.Pendiente, Action<Cita>? ajustar = null)
    {
        var cuenta = await _f.CrearUsuarioAsync(Roles.Dentista);
        return await _f.ConDbAsync(async db =>
        {
            var dentista = new Dentista { UsuarioId = cuenta.Id, Especialidad = "General" };
            var servicio = new Servicio
            {
                Nombre = "Servicio", Precio = 800, DuracionSlots = 1,
                Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") }
            };
            var paciente = new Paciente { Nombre = "Paciente", Apellido = "Prueba", Telefono = "5550001111", Origen = OrigenesCita.Recepcion };
            db.AddRange(dentista, servicio, paciente);
            await db.SaveChangesAsync();
            db.DentistaServicios.Add(new DentistaServicio { DentistaId = dentista.Id, ServicioId = servicio.Id });
            var cita = DentissaFactory.NuevaCita(paciente.Id, dentista.Id, servicio.Id, inicio.UtcDateTime);
            cita.Estado = estado;
            cita.CostoBase = cita.CostoTotal = 800;
            if (estado == EstadosCita.Cancelada)
            {
                cita.MotivoCancelacion = "Prueba";
                cita.CanceladaEn = inicio.UtcDateTime;
            }
            ajustar?.Invoke(cita);
            db.Citas.Add(cita);
            await db.SaveChangesAsync();
            return new Escenario(cita.Id, dentista.Id, servicio.Id, paciente.Id, cuenta);
        });
    }

    private Task<Cita> LeerAsync(int id) => _f.ConDbAsync(db => db.Citas.AsNoTracking().Include(c => c.Historial).SingleAsync(c => c.Id == id));

    private static async Task<string?> CodigoAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("codigo", out var c) ? c.GetString() : null;

    private Task<List<Auditoria>> BitacoraAsync(int cita) => _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
        .Where(a => a.Entidad == "citas" && a.EntidadId == cita.ToString() && a.Accion == AccionesAuditoria.Modificar).ToListAsync());

    // ── Confirmar ────────────────────────────────────────
    [Fact]
    public async Task Confirmar_PorDefectoLlamada_ConCanalEnHistorial_EIdempotente()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);
        var otra = await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otra.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Equal(CanalesCitaHistorial.Llamada, cita.ConfirmacionCanal);
        var h = Assert.Single(cita.Historial, x => x.Accion == AccionesCitaHistorial.Confirmacion);
        Assert.Equal(CanalesCitaHistorial.Llamada, h.Canal);
    }

    [Fact]
    public async Task Confirmar_CanalPanel_YCanalInvalido400()
    {
        var e = await CitaAsync(Local(12, 1, 10));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        Assert.Equal(HttpStatusCode.BadRequest, (await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/confirmar", new { canal = "correo" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/confirmar", new { canal = "panel" })).StatusCode);
        Assert.Equal(CanalesCitaHistorial.Panel, (await LeerAsync(e.Cita)).ConfirmacionCanal);
    }

    [Fact]
    public async Task Confirmar_VencidaAunNoCancelada_SePuede_YDejaDeVencer()
    {
        _f.Reloj.Fijar(Local(11, 28, 12)); // la solicitud se creó el sábado y venció el lunes a las 9:00
        var e = await CitaAsync(Local(11, 30, 12), ajustar: c =>
        {
            c.Origen = OrigenesCita.Web;
            c.BloqueoExpiraEn = Local(11, 30, 9).UtcDateTime;
        });
        _f.Reloj.Fijar(Ahora);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Null(cita.BloqueoExpiraEn);
    }

    [Fact]
    public async Task Confirmar_UnaCancelada_409TransicionInvalida()
    {
        var e = await CitaAsync(Local(12, 1, 9), EstadosCita.Cancelada);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("TRANSICION_INVALIDA", await CodigoAsync(r));
    }

    // ── Cancelar: el motivo solo en la cita ──────────────
    [Fact]
    public async Task Cancelar_HistorialGenerico_YMotivoEnmascaradoEnLaBitacora()
    {
        const string motivo = "Dolor fuerte en una muela, prefiere otro día";
        var e = await CitaAsync(Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/cancelar", new { motivo });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(motivo, cita.MotivoCancelacion);
        var h = Assert.Single(cita.Historial, x => x.Accion == AccionesCitaHistorial.Cancelacion);
        Assert.Equal("Cancelada por recepción", h.Detalle);
        Assert.Equal(CanalesCitaHistorial.Panel, h.Canal);
        Assert.DoesNotContain(cita.Historial, x => (x.Detalle ?? "").Contains("muela") || (x.Datos ?? "").Contains("muela"));
        var fila = Assert.Single(await BitacoraAsync(e.Cita));
        Assert.DoesNotContain("muela", fila.Detalle);
        Assert.Contains(AuditoriaInterceptor.Cambio, fila.Detalle);
    }

    // ── Completar y no asistió ───────────────────────────
    [Fact]
    public async Task Completar_AntesDelInicio409_DespuesSi()
    {
        var futura = await CitaAsync(Local(11, 30, 12));
        var pasada = await CitaAsync(Local(11, 30, 10));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var antes = await recepcion.PatchAsync($"/api/citas/{futura.Cita}/completar", null);
        var despues = await recepcion.PatchAsync($"/api/citas/{pasada.Cita}/completar", null);

        Assert.Equal(HttpStatusCode.Conflict, antes.StatusCode);
        Assert.Equal("TRANSICION_INVALIDA", await CodigoAsync(antes));
        Assert.Equal(HttpStatusCode.OK, despues.StatusCode);
        var cita = await LeerAsync(pasada.Cita);
        Assert.Equal(EstadosCita.Completada, cita.Estado);
        Assert.Contains(cita.Historial, x => x.Accion == AccionesCitaHistorial.CambioEstado && x.Detalle == "pendiente → completada");
    }

    [Fact]
    public async Task NoAsistio_DesdeInicioMasQuinceMinutos()
    {
        var e = await CitaAsync(Local(11, 30, 10)); // ahora 10:30
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        _f.Reloj.Fijar(Local(11, 30, 10, 14));
        var temprano = await recepcion.PatchAsync($"/api/citas/{e.Cita}/no-asistio", null);
        _f.Reloj.Fijar(Local(11, 30, 10, 15));
        var aTiempo = await recepcion.PatchAsync($"/api/citas/{e.Cita}/no-asistio", null);

        Assert.Equal(HttpStatusCode.Conflict, temprano.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aTiempo.StatusCode);
        Assert.Equal(EstadosCita.NoAsistio, (await LeerAsync(e.Cita)).Estado);
    }

    [Fact]
    public async Task Dentista_SoloEnSusCitas_LasAjenas404()
    {
        var mia = await CitaAsync(Local(11, 30, 9));
        var ajena = await CitaAsync(Local(11, 30, 9));
        var dentista = await _f.ClienteDeAsync(mia.CuentaDentista);

        Assert.Equal(HttpStatusCode.OK, (await dentista.PatchAsync($"/api/citas/{mia.Cita}/completar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await dentista.PatchAsync($"/api/citas/{ajena.Cita}/no-asistio", null)).StatusCode);
        Assert.Equal(EstadosCita.Pendiente, (await LeerAsync(ajena.Cita)).Estado);
        // El dentista no confirma, no cancela ni reagenda
        Assert.Equal(HttpStatusCode.Forbidden, (await dentista.PatchAsync($"/api/Citas/{mia.Cita}/confirmar", null)).StatusCode);
    }

    [Theory]
    [InlineData(Roles.Paciente, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Completar_PacienteYAnonimo_NoPueden(string? rol, HttpStatusCode esperado)
    {
        var e = await CitaAsync(Local(11, 30, 9));
        var client = rol is null ? _f.CreateClient() : await _f.ClienteConRolAsync(rol);

        Assert.Equal(esperado, (await client.PatchAsync($"/api/citas/{e.Cita}/completar", null)).StatusCode);
    }

    // ── Corrección de administración ─────────────────────
    [Fact]
    public async Task Corregir_SoloAdmin_ConMotivo_DentroDeSieteDias()
    {
        var e = await CitaAsync(Local(11, 25, 9), EstadosCita.Completada);
        var admin = await _f.ClienteConRolAsync(Roles.Admin);
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        Assert.Equal(HttpStatusCode.Forbidden, (await asistente.PatchAsJsonAsync($"/api/Citas/{e.Cita}/corregir-estado", new { estado = "no_asistio", motivo = "Error al marcar" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/Citas/{e.Cita}/corregir-estado", new { estado = "no_asistio", motivo = "" })).StatusCode);
        var r = await admin.PatchAsJsonAsync($"/api/Citas/{e.Cita}/corregir-estado", new { estado = "no_asistio", motivo = "Se marcó por error" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(EstadosCita.NoAsistio, cita.Estado);
        Assert.Equal("Se marcó por error", cita.MotivoCambio);
        var h = Assert.Single(cita.Historial, x => x.Accion == AccionesCitaHistorial.CorreccionEstado);
        Assert.DoesNotContain("error", h.Detalle!);
        Assert.DoesNotContain("error", Assert.Single(await BitacoraAsync(e.Cita)).Detalle);
    }

    [Fact]
    public async Task Corregir_FueraDeSieteDiasODesdePendiente_409()
    {
        var vieja = await CitaAsync(Local(11, 20, 9), EstadosCita.Completada);
        var pendiente = await CitaAsync(Local(11, 30, 9));
        var admin = await _f.ClienteConRolAsync(Roles.Admin);

        var r1 = await admin.PatchAsJsonAsync($"/api/Citas/{vieja.Cita}/corregir-estado", new { estado = "no_asistio", motivo = "Tarde" });
        var r2 = await admin.PatchAsJsonAsync($"/api/Citas/{pendiente.Cita}/corregir-estado", new { estado = "completada", motivo = "No aplica" });

        Assert.Equal(HttpStatusCode.Conflict, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    // ── Reagenda ─────────────────────────────────────────
    [Fact]
    public async Task Reagendar_QuedaConfirmada_ConservaElPrecio_YHistorial()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        await _f.ConDbAsync(db => db.Servicios.Where(s => s.Id == e.Servicio).ExecuteUpdateAsync(s => s.SetProperty(x => x.Precio, 1500)));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/reagendar", new { fecha = "2026-12-02", hora = "13:00:00", motivo = "Pidió otro día" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(Local(12, 2, 13).UtcDateTime, cita.Inicio);
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Equal(800, cita.CostoBase);
        Assert.Equal("Pidió otro día", cita.MotivoCambio);
        var h = Assert.Single(cita.Historial, x => x.Accion == AccionesCitaHistorial.Reagenda);
        Assert.Equal("Reagendada por recepción", h.Detalle);
    }

    [Fact]
    public async Task Reagendar_ComoPendiente_YSobreOcupado409_YEnElPasado400()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        await _f.ConDbAsync(db =>
        {
            db.Citas.Add(DentissaFactory.NuevaCita(e.Paciente, e.Dentista, e.Servicio, Local(12, 1, 12).UtcDateTime));
            return db.SaveChangesAsync();
        });
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var ocupado = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/reagendar", new { fecha = "2026-12-01", hora = "12:00:00" });
        var pasado = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/reagendar", new { fecha = "2026-11-30", hora = "09:00:00" });
        var pendiente = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/reagendar", new { fecha = "2026-12-01", hora = "13:00:00", estadoResultante = "pendiente" });

        Assert.Equal(HttpStatusCode.Conflict, ocupado.StatusCode);
        Assert.Equal("HORARIO_OCUPADO", await CodigoAsync(ocupado));
        Assert.Equal(HttpStatusCode.BadRequest, pasado.StatusCode);
        Assert.Equal("HORARIO_PASADO", await CodigoAsync(pasado));
        Assert.Equal(HttpStatusCode.OK, pendiente.StatusCode);
        Assert.Equal(EstadosCita.Pendiente, (await LeerAsync(e.Cita)).Estado);
    }

    [Fact]
    public async Task Reagendar_SobreUnaVencida_LaLibera_ANombreDelSistema()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        _f.Reloj.Fijar(Local(11, 28, 12));
        var vencida = await _f.ConDbAsync(async db =>
        {
            var c = DentissaFactory.NuevaCita(e.Paciente, e.Dentista, e.Servicio, Local(12, 1, 15).UtcDateTime);
            c.Origen = OrigenesCita.Web;
            c.BloqueoExpiraEn = Local(11, 30, 10).UtcDateTime; // ya venció ("ahora" 10:30)
            db.Citas.Add(c);
            await db.SaveChangesAsync();
            return c.Id;
        });
        _f.Reloj.Fijar(Ahora);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/reagendar", new { fecha = "2026-12-01", hora = "15:00:00" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(EstadosCita.Cancelada, (await LeerAsync(vencida)).Estado);
        var fila = Assert.Single(await BitacoraAsync(vencida));
        Assert.Equal(ContextoAuditoria.ActorSistema, fila.Actor);
        Assert.Equal(e.Cita, JsonDocument.Parse(fila.ProvocadoPor!).RootElement.GetProperty("citaId").GetInt32());
    }

    // ── Costos ───────────────────────────────────────────
    [Fact]
    public async Task Costos_RecalculaElTotal_YUnaCanceladaNo()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        var cancelada = await CitaAsync(Local(12, 1, 9), EstadosCita.Cancelada);
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/costos", new { costoExtra = 250, motivoExtra = "Material adicional" });
        var no = await recepcion.PatchAsJsonAsync($"/api/Citas/{cancelada.Cita}/costos", new { costoExtra = 250 });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1050, (await LeerAsync(e.Cita)).CostoTotal);
        Assert.Equal(HttpStatusCode.Conflict, no.StatusCode);
    }

    // ── Concurrencia optimista (xmin) ────────────────────
    [Fact]
    public async Task VersionVieja_409CitaModificada_SinCambios()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        var leida = await recepcion.GetFromJsonAsync<JsonElement>($"/api/Citas/{e.Cita}");
        var version = leida.GetProperty("version").GetUInt32();

        var primera = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/confirmar", new { version });
        var segunda = await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/cancelar", new { motivo = "Ya no puede", version });

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal("CITA_MODIFICADA", await CodigoAsync(segunda));
        Assert.Equal(EstadosCita.Confirmada, (await LeerAsync(e.Cita)).Estado);
        var nueva = (await primera.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetUInt32();
        Assert.NotEqual(version, nueva);
        Assert.Equal(HttpStatusCode.OK, (await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/cancelar", new { motivo = "Ya no puede", version = nueva })).StatusCode);
    }

    // ── Alta ya confirmada ───────────────────────────────
    [Fact]
    public async Task ConfirmarAlCrear_QuedaConfirmada_CanalPanel()
    {
        var (dentista, servicio, paciente) = await _f.DentistaConServicioAsync();
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PostAsJsonAsync("/api/Citas", new { pacienteId = paciente, dentistaId = dentista, servicioId = servicio, fecha = "2026-12-01", hora = "09:00:00", confirmarAlCrear = true });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var cita = await _f.ConDbAsync(db => db.Citas.AsNoTracking().Include(c => c.Historial).SingleAsync(c => c.DentistaId == dentista));
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Equal(CanalesCitaHistorial.Panel, cita.ConfirmacionCanal);
        Assert.NotNull(cita.ConfirmadaPor);
        Assert.Equal([AccionesCitaHistorial.Creacion, AccionesCitaHistorial.Confirmacion], cita.Historial.OrderBy(h => h.Id).Select(h => h.Accion));
    }

    // ── PUT de compatibilidad ────────────────────────────
    [Fact]
    public async Task Put_Compatible_ReagendaYConfirma_YNoVuelveAPendiente()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await recepcion.PutAsJsonAsync($"/api/Citas/{e.Cita}", new { dentistaId = e.Dentista, fecha = "2026-12-01", hora = "10:00:00", estado = "confirmada" });
        var regreso = await recepcion.PutAsJsonAsync($"/api/Citas/{e.Cita}", new { dentistaId = e.Dentista, fecha = "2026-12-01", hora = "10:00:00", estado = "pendiente" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cita = await LeerAsync(e.Cita);
        Assert.Equal(Local(12, 1, 10).UtcDateTime, cita.Inicio);
        Assert.Equal(EstadosCita.Confirmada, cita.Estado);
        Assert.Equal(HttpStatusCode.Conflict, regreso.StatusCode);
    }

    // ── Historial ────────────────────────────────────────
    [Fact]
    public async Task Historial_DeLaCita_ParaRecepcion()
    {
        var e = await CitaAsync(Local(12, 1, 9));
        var recepcion = await _f.ClienteConRolAsync(Roles.Asistente);
        await recepcion.PatchAsync($"/api/Citas/{e.Cita}/confirmar", null);
        await recepcion.PatchAsJsonAsync($"/api/Citas/{e.Cita}/cancelar", new { motivo = "Viaje imprevisto" });

        var r = await recepcion.GetFromJsonAsync<JsonElement>($"/api/Citas/{e.Cita}/historial");
        var dentista = await _f.ClienteDeAsync(e.CuentaDentista);

        Assert.Equal([AccionesCitaHistorial.Confirmacion, AccionesCitaHistorial.Cancelacion], r.EnumerateArray().Select(h => h.GetProperty("accion").GetString()));
        Assert.DoesNotContain("Viaje", r.GetRawText());
        Assert.Equal(HttpStatusCode.Forbidden, (await dentista.GetAsync($"/api/Citas/{e.Cita}/historial")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await recepcion.GetAsync("/api/Citas/999999/historial")).StatusCode);
    }

    // ── CHECK de canales ─────────────────────────────────
    [Fact]
    public async Task Check_CanalDeHistorialDesconocido_Rechazado()
    {
        var e = await CitaAsync(Local(12, 1, 9));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _f.ConDbAsync(db =>
        {
            db.CitaHistorial.Add(new CitaHistorial { CitaId = e.Cita, Accion = AccionesCitaHistorial.Modificacion, Canal = "fax", CreadoEn = DateTime.UtcNow });
            return db.SaveChangesAsync();
        }));

        Assert.Equal("ck_cita_historial_canal", (ex.InnerException as PostgresException)?.ConstraintName);
    }
}
