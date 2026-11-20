using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using backend.Constants;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class AutorizacionTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public AutorizacionTests(DentissaFactory factory) => _factory = factory;

    [Theory]
    [InlineData("GET", "/api/Usuarios")]
    [InlineData("POST", "/api/Usuarios")]
    [InlineData("DELETE", "/api/Usuarios/1")]
    [InlineData("GET", "/api/Citas")]
    [InlineData("GET", "/api/Citas/1")]
    [InlineData("GET", "/api/Citas/pacientes")]
    [InlineData("POST", "/api/Citas")]
    [InlineData("PATCH", "/api/Citas/1/confirmar")]
    [InlineData("POST", "/api/catalogo/imagenes")]
    [InlineData("POST", "/api/auth/logout")]
    public async Task SinSesion_Devuelve401(string metodo, string ruta)
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        if (metodo is "POST" or "PUT") req.Content = JsonContent.Create(new { });

        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task TokenInvalido_Devuelve401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "no-es-un-jwt");

        var res = await client.GetAsync("/api/Citas");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Paciente, "/api/Usuarios")]
    [InlineData(Roles.Paciente, "/api/Citas")]
    [InlineData(Roles.Paciente, "/api/Citas/servicios")]
    [InlineData(Roles.Asistente, "/api/Usuarios")]
    [InlineData(Roles.Dentista, "/api/Usuarios")]
    [InlineData(Roles.Dentista, "/api/Citas")]
    [InlineData(Roles.Dentista, "/api/Citas/pacientes")]
    [InlineData(Roles.Asistente, "/api/citas/mi-agenda")]
    [InlineData(Roles.Paciente, "/api/citas/mi-agenda")]
    [InlineData(Roles.Admin, "/api/citas/mi-agenda")]
    public async Task RolIncorrecto_Devuelve403(string rol, string ruta)
    {
        var client = await _factory.ClienteConRolAsync(rol);

        var res = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin, "/api/Usuarios")]
    [InlineData(Roles.Admin, "/api/Citas")]
    [InlineData(Roles.Asistente, "/api/Citas")]
    [InlineData(Roles.Asistente, "/api/Citas/servicios")]
    public async Task RolCorrecto_Devuelve200(string rol, string ruta)
    {
        var client = await _factory.ClienteConRolAsync(rol);

        var res = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Asistente)]
    [InlineData(Roles.Paciente)]
    public async Task SoloAdminPuedeCrearAdmin(string rol)
    {
        var client = await _factory.ClienteConRolAsync(rol);
        var email = $"nuevo-admin-{Guid.NewGuid():N}@prueba.test";

        var res = await client.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "X", apellido = "Y", email, password = "Segura123!", rol = Roles.Admin
        });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.False(await _factory.ConDbAsync(db => db.Usuarios.AnyAsync(u => u.Correo == email)));
    }

    [Fact]
    public async Task AdminPuedeCrearAdmin()
    {
        var client = await _factory.ClienteConRolAsync(Roles.Admin);

        var res = await client.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "X", apellido = "Y", email = $"admin2-{Guid.NewGuid():N}@prueba.test",
            password = "Segura123!", rol = Roles.Admin
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}

public class RegistroTests : IClassFixture<DentissaFactory>
{
    private readonly DentissaFactory _factory;

    public RegistroTests(DentissaFactory factory) => _factory = factory;

    [Theory]
    [InlineData("admin")]
    [InlineData("asistente")]
    [InlineData("dentista")]
    [InlineData("ADMIN")]
    public async Task Register_IgnoraElRolEnviado(string rolEnviado)
    {
        var client = _factory.CreateClient();
        var email = $"registro-{Guid.NewGuid():N}@prueba.test";

        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Ana", apellido = "López", email, telefono = "5550000000",
            password = "Segura123!", rol = rolEnviado
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cuerpo = await res.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal(Roles.Paciente, cuerpo!["rol"].ToString());

        var rolGuardado = await _factory.ConDbAsync(db => db.Usuarios.Where(u => u.Correo == email).Select(u => u.Rol).SingleAsync());
        Assert.Equal(Roles.Paciente, rolGuardado);
    }
}

public class DtosTests : IClassFixture<DentissaFactory>
{
    // Formato de hash BCrypt ($2a$, $2b$, $2y$)
    private static readonly Regex HashBcrypt = new(@"\$2[aby]\$\d{2}\$");

    private readonly DentissaFactory _factory;

    public DtosTests(DentissaFactory factory) => _factory = factory;

    private static void AssertSinDatosDeCuenta(string json)
    {
        Assert.DoesNotContain("\"password\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(HashBcrypt, json);
        Assert.DoesNotContain("refresh-de-prueba-", json);
        Assert.DoesNotContain("refreshTokenExpiration", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CitaPorId_NoExponeDatosDeCuentaNiPersonales()
    {
        var citaId = await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Admin);

        var res = await client.GetAsync($"/api/Citas/{citaId}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        AssertSinDatosDeCuenta(json);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@prueba.test", json);
        Assert.DoesNotContain("curp", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"paciente\"", json);
    }

    [Theory]
    [InlineData("/api/Citas")]
    [InlineData("/api/Citas/pacientes")]
    [InlineData("/api/Citas/dentistas")]
    [InlineData("/api/Usuarios")]
    public async Task Listados_NoExponenHashNiTokens(string ruta)
    {
        await _factory.CrearCitaAsync();
        var client = await _factory.ClienteConRolAsync(Roles.Admin);

        var res = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        AssertSinDatosDeCuenta(json);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearUsuario_NoDevuelveHash()
    {
        var client = await _factory.ClienteConRolAsync(Roles.Admin);

        var res = await client.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "X", apellido = "Y", email = $"u-{Guid.NewGuid():N}@prueba.test",
            password = "Segura123!", rol = Roles.Asistente
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        AssertSinDatosDeCuenta(await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoginYRegistro_NoDevuelvenHash()
    {
        var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@prueba.test";

        var registro = await client.PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Ana", apellido = "López", email, password = "Segura123!"
        });
        AssertSinDatosDeCuenta(await registro.Content.ReadAsStringAsync());

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Segura123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        // El login sí devuelve el refresh token de la sesión nueva, pero nunca el hash
        AssertSinDatosDeCuenta(await login.Content.ReadAsStringAsync());
    }
}

public class RateLimitTests : IClassFixture<DentissaFactoryLimitesReales>
{
    private readonly DentissaFactoryLimitesReales _factory;

    public RateLimitTests(DentissaFactoryLimitesReales factory) => _factory = factory;

    [Fact]
    public async Task Login_SextoIntentoConMismoCorreo_Devuelve429()
    {
        var client = _factory.CreateClient();
        var email = $"fuerza-bruta-{Guid.NewGuid():N}@prueba.test";

        for (var i = 0; i < 5; i++)
        {
            var intento = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mal" });
            Assert.Equal(HttpStatusCode.Unauthorized, intento.StatusCode);
        }

        // Mismo correo con mayúsculas y espacios: cuenta como el mismo
        var sexto = await client.PostAsJsonAsync("/api/auth/login", new { email = " " + email.ToUpperInvariant(), password = "mal" });
        Assert.Equal((HttpStatusCode)429, sexto.StatusCode);
        Assert.True(sexto.Headers.Contains("Retry-After"));

        // Otro correo desde la misma IP aún puede intentar (el tope por IP es 30)
        var otro = await client.PostAsJsonAsync("/api/auth/login", new { email = $"otro-{Guid.NewGuid():N}@prueba.test", password = "mal" });
        Assert.Equal(HttpStatusCode.Unauthorized, otro.StatusCode);
    }
}

public class ArranqueTests
{
    [Fact]
    public void SinClaveJwt_LaAplicacionNoArranca()
    {
        using var factory = new DentissaFactorySinClaveJwt();

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("JwtSettings:SecretKey", ex.ToString());
    }
}

public class CatalogoImagenesTests : IClassFixture<DentissaFactory>
{
    private static readonly byte[] PngMinimo = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private readonly DentissaFactory _factory;

    public CatalogoImagenesTests(DentissaFactory factory) => _factory = factory;

    private static MultipartFormDataContent Formulario(byte[] bytes, string nombre, string tipo)
    {
        var archivo = new ByteArrayContent(bytes);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        return new MultipartFormDataContent { { archivo, "archivo", nombre } };
    }

    [Fact]
    public async Task Admin_SubePng_YSeLeeSinSesion()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);

        var res = await admin.PostAsync("/api/catalogo/imagenes", Formulario(PngMinimo, "../../foto.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cuerpo = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Matches("^[0-9a-f]{32}\\.png$", cuerpo!["nombre"]);

        var publico = await _factory.CreateClient().GetAsync(cuerpo["url"]);
        Assert.Equal(HttpStatusCode.OK, publico.StatusCode);
        Assert.Equal("image/png", publico.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Asistente_NoPuedeSubir()
    {
        var client = await _factory.ClienteConRolAsync(Roles.Asistente);

        var res = await client.PostAsync("/api/catalogo/imagenes", Formulario(PngMinimo, "foto.png", "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [InlineData("script.png", "image/png")]  // contenido que no es imagen con extensión .png
    [InlineData("foto.gif", "image/gif")]     // formato no permitido
    public async Task RechazaArchivosNoValidos(string nombre, string tipo)
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var bytes = nombre.EndsWith(".gif") ? "GIF89a"u8.ToArray() : "<script>alert(1)</script>"u8.ToArray();

        var res = await admin.PostAsync("/api/catalogo/imagenes", Formulario(bytes, nombre, tipo));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task RechazaMasDe5MB()
    {
        var admin = await _factory.ClienteConRolAsync(Roles.Admin);
        var grande = new byte[5 * 1024 * 1024 + 1];
        PngMinimo.CopyTo(grande, 0);

        var res = await admin.PostAsync("/api/catalogo/imagenes", Formulario(grande, "grande.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData("..%2F..%2Fappsettings.json")]
    [InlineData("appsettings.json")]
    [InlineData("00000000000000000000000000000000.png")]
    public async Task Get_NombreNoValidoOInexistente_Devuelve404(string nombre)
    {
        var res = await _factory.CreateClient().GetAsync($"/api/catalogo/imagenes/{nombre}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Respuestas_IncluyenCabecerasDeSeguridad()
    {
        var res = await _factory.CreateClient().GetAsync("/api/catalogo/imagenes/no-existe.png");

        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(res.Headers.Contains("Content-Security-Policy"));
    }
}
