using System.Net;
using System.Net.Http.Json;
using backend.Constants;

namespace backend.Tests;

// GET /api/Usuarios: último acceso (solo administración) y sin datos sensibles
public class UsuariosListaTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public UsuariosListaTests(DentissaFactory factory) => _factory = factory;

    private static async Task<Dictionary<string, object?>> FilaDeAsync(HttpClient client, string correo)
    {
        var res = await client.GetAsync("/api/Usuarios");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var filas = await res.Content.ReadFromJsonAsync<List<Dictionary<string, object?>>>();
        return filas!.Single(f => f["email"]?.ToString() == correo);
    }

    [Fact]
    public async Task UltimoAcceso_EsNullSiNuncaEntro()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var nuevo = await _factory.CrearUsuarioAsync(Roles.Asistente);

        var fila = await FilaDeAsync(admin, nuevo.Correo);

        Assert.True(fila.ContainsKey("ultimoAccesoEn"));
        Assert.Null(fila["ultimoAccesoEn"]);
    }

    [Fact]
    public async Task UltimoAcceso_SeLlenaAlIniciarSesion_EnUtc()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var asistente = await _factory.CrearUsuarioAsync(Roles.Asistente);
        var antes = DateTime.UtcNow.AddSeconds(-5);

        await _factory.ClienteDeAsync(asistente);
        var fila = await FilaDeAsync(admin, asistente.Correo);

        var texto = fila["ultimoAccesoEn"]!.ToString()!;
        Assert.EndsWith("Z", texto);
        var instante = DateTime.Parse(texto, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.InRange(instante, antes, DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task Lista_NoExponeHashNiTokens()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var otro = await _factory.CrearUsuarioAsync(Roles.Dentista);

        var fila = await FilaDeAsync(admin, otro.Correo);

        foreach (var clave in fila.Keys)
        {
            Assert.DoesNotContain("password", clave, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", clave, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(Roles.Asistente)]
    [InlineData(Roles.Dentista)]
    [InlineData(Roles.Paciente)]
    public async Task SoloAdministracion_VeLaLista(string rol)
    {
        var client = await _factory.ClienteConRolAsync(rol);

        var res = await client.GetAsync("/api/Usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
