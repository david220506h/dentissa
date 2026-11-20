using System.Net;
using System.Net.Http.Json;
using backend.Constants;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class MiAgendaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public MiAgendaTests(DentissaFactory factory) => _factory = factory;

    [Fact]
    public async Task SinSesion_Devuelve401()
    {
        var res = await _factory.CreateClient().GetAsync("/api/citas/mi-agenda");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Dentista_SoloVeSusCitas()
    {
        var (miCita, yo) = await _factory.CrearCitaConDentistaAsync();
        var (otraCita, _) = await _factory.CrearCitaConDentistaAsync();
        var client = await _factory.ClienteDeAsync(yo);

        var res = await client.GetAsync("/api/citas/mi-agenda");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var citas = await res.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var ids = citas!.Select(c => int.Parse(c["id"].ToString()!)).ToList();
        Assert.Contains(miCita, ids);
        Assert.DoesNotContain(otraCita, ids);
    }

    [Fact]
    public async Task Dentista_NoPuedeEscribirCitas()
    {
        var (citaId, yo) = await _factory.CrearCitaConDentistaAsync();
        var client = await _factory.ClienteDeAsync(yo);

        var confirmar = await client.PatchAsync($"/api/Citas/{citaId}/confirmar", null);

        Assert.Equal(HttpStatusCode.Forbidden, confirmar.StatusCode);
    }

    [Fact]
    public async Task RangoInvertido_Devuelve400()
    {
        var (_, yo) = await _factory.CrearCitaConDentistaAsync();
        var client = await _factory.ClienteDeAsync(yo);

        var res = await client.GetAsync("/api/citas/mi-agenda?desde=2026-12-10&hasta=2026-12-01");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}

public class FichasPorRolTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public FichasPorRolTests(DentissaFactory factory) => _factory = factory;

    [Fact]
    public async Task CrearUsuarioDentista_CreaSuFicha()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var email = $"dentista-{Guid.NewGuid():N}@prueba.test";

        var res = await admin.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "Diana", apellido = "Ruiz", email, password = "Segura123!",
            rol = Roles.Dentista, cedulaProfesional = "1234567", especialidad = "Endodoncia"
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var ficha = await _factory.ConDbAsync(db => db.Dentistas.SingleAsync(d => d.Usuario.Correo == email));
        Assert.Equal("1234567", ficha.CedulaProfesional);
        Assert.True(ficha.Activo);
    }

    [Fact]
    public async Task CrearUsuarioPaciente_CreaSuFichaConOrigenRecepcion()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var email = $"paciente-{Guid.NewGuid():N}@prueba.test";

        var res = await admin.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "Pablo", apellido = "Mora", email, password = "Segura123!", rol = Roles.Paciente
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var ficha = await _factory.ConDbAsync(db => db.Pacientes.SingleAsync(p => p.Usuario!.Correo == email));
        Assert.Equal("recepcion", ficha.Origen);
        Assert.Equal(email, ficha.Correo);
    }

    [Fact]
    public async Task Registro_CreaFichaDePacienteConOrigenPortal()
    {
        var email = $"portal-{Guid.NewGuid():N}@prueba.test";

        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Ana", apellido = "López", email, telefono = "5550000000", password = "Segura123!"
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var ficha = await _factory.ConDbAsync(db => db.Pacientes.SingleAsync(p => p.Usuario!.Correo == email));
        Assert.Equal("portal", ficha.Origen);
        Assert.Equal("5550000000", ficha.Telefono);
        Assert.False(ficha.ContactoVerificado);
    }
}
