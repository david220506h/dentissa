using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Constants;
using backend.Entities;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

// Fase 2b: pacientes para el panel (datos ficticios). Permisos, búsqueda, bitácora de lecturas y escrituras.
public class PacientesTests : IClassFixture<FabricaDisponibilidad>
{
    private readonly FabricaDisponibilidad _f;

    public PacientesTests(FabricaDisponibilidad f)
    {
        _f = f;
        _f.Reloj.Fijar(new DateTimeOffset(2026, 11, 30, 10, 0, 0, TimeSpan.FromHours(-6)));
    }

    private static DateTime Local(int mes, int dia, int hora) => new DateTimeOffset(2026, mes, dia, hora, 0, 0, TimeSpan.FromHours(-6)).UtcDateTime;

    private Task<int> PacienteAsync(string nombre, string apellido, string? telefono = "5550001111", string? alergias = null) => _f.ConDbAsync(async db =>
    {
        var p = new Paciente { Nombre = nombre, Apellido = apellido, Telefono = telefono, Correo = telefono is null ? $"p-{Guid.NewGuid():N}@prueba.test" : null, Alergias = alergias, Origen = OrigenesCita.Recepcion };
        db.Pacientes.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    });

    private async Task<(int Dentista, int Servicio, Usuario Cuenta)> DentistaAsync()
    {
        var cuenta = await _f.CrearUsuarioAsync(Roles.Dentista);
        var (dentista, servicio) = await _f.ConDbAsync(async db =>
        {
            var d = new Dentista { UsuarioId = cuenta.Id, Especialidad = "General" };
            var s = new Servicio { Nombre = "Revisión", Precio = 300, DuracionSlots = 1, Categoria = new CategoriaServicio { Nombre = "Cat-" + Guid.NewGuid().ToString("N") } };
            db.AddRange(d, s);
            await db.SaveChangesAsync();
            return (d.Id, s.Id);
        });
        return (dentista, servicio, cuenta);
    }

    private Task CitaAsync(int paciente, int dentista, int servicio, DateTime inicio, string estado = EstadosCita.Pendiente) => _f.ConDbAsync(db =>
    {
        var c = DentissaFactory.NuevaCita(paciente, dentista, servicio, inicio);
        c.Estado = estado;
        if (estado == EstadosCita.Cancelada) { c.MotivoCancelacion = "Prueba"; c.CanceladaEn = inicio; }
        db.Citas.Add(c);
        return db.SaveChangesAsync();
    });

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Busqueda_SinAcentosYPorTelefono_Paginada_YOrdenada()
    {
        var marca = Guid.NewGuid().ToString("N")[..6];
        var nunez = await PacienteAsync("Ramona", $"Núñez{marca}", "55 1234 9876");
        await PacienteAsync("Ana", $"Zeta{marca}");
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        var porNombre = await JsonAsync(await asistente.GetAsync($"/api/pacientes?buscar=nunez{marca}"));
        var porTelefono = await JsonAsync(await asistente.GetAsync("/api/pacientes?buscar=1234-9876"));
        var pagina = await JsonAsync(await asistente.GetAsync($"/api/pacientes?buscar={marca}&tamano=1&pagina=2"));

        Assert.Equal([nunez], porNombre.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));
        Assert.Contains(nunez, porTelefono.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()));
        Assert.Equal(2, pagina.GetProperty("total").GetInt32());
        Assert.Equal($"Zeta{marca}", pagina.GetProperty("elementos")[0].GetProperty("apellido").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await asistente.GetAsync("/api/pacientes?buscar=a")).StatusCode);
    }

    [Fact]
    public async Task Asistente_VeAlergias_YNuncaDatosDelExpediente_NiCuenta()
    {
        var id = await PacienteAsync("Luz", "Alergica", alergias: "Penicilina");
        await _f.ConDbAsync(db => db.Pacientes.Where(p => p.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(p => p.Curp, "XEXX010101HNEXXXA4").SetProperty(p => p.Direccion, "Calle Ficticia 1").SetProperty(p => p.CondicionesMedicas, "Ficticia")));
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        var r = await asistente.GetAsync($"/api/pacientes/{id}");
        var texto = await r.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Penicilina", (await JsonAsync(r)).GetProperty("alergias").GetString());
        foreach (var prohibido in new[] { "curp", "direccion", "condiciones", "XEXX", "Ficticia", "passwordHash", "refreshToken" })
            Assert.DoesNotContain(prohibido, texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dentista_SoloSusAsignados_SinTelefono_YLosDemas404()
    {
        var (dentista, servicio, cuenta) = await DentistaAsync();
        var asignado = await PacienteAsync("Asignada", "Paciente");
        var cancelado = await PacienteAsync("Cancelada", "Paciente");
        var ajeno = await PacienteAsync("Ajena", "Paciente");
        await CitaAsync(asignado, dentista, servicio, Local(12, 2, 9));
        await CitaAsync(cancelado, dentista, servicio, Local(12, 2, 10), EstadosCita.Cancelada);
        var client = await _f.ClienteDeAsync(cuenta);

        var lista = await JsonAsync(await client.GetAsync("/api/pacientes?tamano=100"));
        var ids = lista.GetProperty("elementos").EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToList();

        Assert.Equal([asignado], ids);
        Assert.Equal(JsonValueKind.Null, lista.GetProperty("elementos")[0].GetProperty("telefono").ValueKind);
        Assert.Equal("2026-12-02", lista.GetProperty("elementos")[0].GetProperty("proximaCita").GetProperty("fecha").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/pacientes/{asignado}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pacientes/{cancelado}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pacientes/{ajeno}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/pacientes", new { nombre = "X", apellido = "Y", telefono = "5550009999" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/pacientes/{asignado}/alergias", new { alergias = "Látex" })).StatusCode);
    }

    [Theory]
    [InlineData(Roles.Paciente, HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task PacienteYAnonimo_NoEntran(string? rol, HttpStatusCode esperado)
    {
        var client = rol is null ? _f.CreateClient() : await _f.ClienteConRolAsync(rol);

        Assert.Equal(esperado, (await client.GetAsync("/api/pacientes")).StatusCode);
    }

    [Fact]
    public async Task LecturaConAlergias_QuedaEnLaBitacora_SinValoresNiBusqueda()
    {
        var marca = Guid.NewGuid().ToString("N")[..8];
        var id = await PacienteAsync("Lectura", $"Bitacora{marca}", alergias: "Sulfas");
        var cuenta = await _f.CrearUsuarioAsync(Roles.Asistente);
        var client = await _f.ClienteDeAsync(cuenta);

        await client.GetAsync($"/api/pacientes?buscar=bitacora{marca}");
        await client.GetAsync($"/api/pacientes/{id}");

        var filas = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .Where(a => a.Accion == AccionesAuditoria.LecturaSensible && a.UsuarioId == cuenta.Id).OrderBy(a => a.Id).ToListAsync());
        Assert.Equal(2, filas.Count);
        Assert.Null(filas[0].EntidadId);
        Assert.Contains(id.ToString(), filas[0].Detalle);
        Assert.True(JsonDocument.Parse(filas[0].Detalle!).RootElement.GetProperty("busqueda").GetBoolean());
        Assert.Equal(id.ToString(), filas[1].EntidadId);
        Assert.All(filas, f =>
        {
            Assert.DoesNotContain("Sulfas", f.Detalle);
            Assert.DoesNotContain(marca, f.Detalle, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Alta_Edicion_Alergias_YBitacoraEnmascarada()
    {
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        var sinContacto = await asistente.PostAsJsonAsync("/api/pacientes", new { nombre = "Sin", apellido = "Contacto" });
        var telefonoCorto = await asistente.PostAsJsonAsync("/api/pacientes", new { nombre = "Tel", apellido = "Corto", telefono = "123" });
        var alta = await asistente.PostAsJsonAsync("/api/pacientes", new { nombre = "Nueva", apellido = "Ficticia", telefono = "55 5000 1234", sexo = "otro", alergias = "Ibuprofeno" });

        Assert.Equal(HttpStatusCode.BadRequest, sinContacto.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, telefonoCorto.StatusCode);
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var id = (await JsonAsync(alta)).GetProperty("id").GetInt32();

        var edicion = await asistente.PutAsJsonAsync($"/api/pacientes/{id}", new { nombre = "Nueva", apellido = "Editada", correo = "nueva@prueba.test", telefono = "55 5000 1234", alergias = "Ibuprofeno" });
        var alergias = await asistente.PatchAsJsonAsync($"/api/pacientes/{id}/alergias", new { alergias = "Látex" });

        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        Assert.Equal("Látex", (await JsonAsync(alergias)).GetProperty("alergias").GetString());
        var bitacora = await _f.ConDbAsync(db => db.Auditoria.AsNoTracking()
            .Where(a => a.Entidad == "pacientes" && a.EntidadId == id.ToString() && a.Accion != AccionesAuditoria.LecturaSensible).ToListAsync());
        Assert.Contains(bitacora, a => a.Accion == AccionesAuditoria.Crear);
        Assert.All(bitacora, a =>
        {
            Assert.DoesNotContain("Ibuprofeno", a.Detalle ?? "");
            Assert.DoesNotContain("Látex", a.Detalle ?? "");
        });
        Assert.Contains(bitacora, a => (a.Detalle ?? "").Contains(AuditoriaInterceptor.Cambio));
    }

    [Fact]
    public async Task Baja_Logica_YConCitasProximas409()
    {
        var (dentista, servicio, _) = await DentistaAsync();
        var libre = await PacienteAsync("Baja", "Libre");
        var conCita = await PacienteAsync("Baja", "ConCita");
        await CitaAsync(conCita, dentista, servicio, Local(12, 3, 9));
        var asistente = await _f.ClienteConRolAsync(Roles.Asistente);

        Assert.Equal(HttpStatusCode.Conflict, (await asistente.DeleteAsync($"/api/pacientes/{conCita}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await asistente.DeleteAsync($"/api/pacientes/{libre}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await asistente.GetAsync($"/api/pacientes/{libre}")).StatusCode);
        var fila = await _f.ConDbAsync(db => db.Pacientes.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == libre));
        Assert.NotNull(fila.EliminadoEn);
        Assert.True(await _f.ConDbAsync(db => db.Auditoria.AnyAsync(a => a.Entidad == "pacientes" && a.EntidadId == libre.ToString() && a.Accion == AccionesAuditoria.Eliminar)));
    }
}
