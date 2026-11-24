using System.Text;
using backend.Entities;
using backend.Repositories;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros servicios) indican el puerto en $PORT
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } puerto)
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

// Los secretos vienen de user-secrets (desarrollo) o variables de entorno (producción).
// Si faltan, la aplicación no arranca.
// Acepta también la URI que entrega Neon (postgresql://usuario:clave@host/base?sslmode=require)
var connectionString = CadenaConexion.Normalizar(builder.Configuration.GetConnectionString("DefaultConnection"));
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configúrala con user-secrets o con la variable de entorno ConnectionStrings__DefaultConnection.");

// Bitácora: el interceptor es por petición (lleva el actor y la transacción propia)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorActual, ActorActual>();
builder.Services.AddScoped<ContextoAuditoria>();
builder.Services.AddScoped<AuditoriaInterceptor>();

// Conexión PostgreSQL; tablas y columnas en snake_case
builder.Services.AddDbContext<DentissaDbContext>((sp, options) =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()
           .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>())
           // Intencional: las consultas de citas ignoran el filtro de borrado lógico
           // (CitasRepository) para no perder citas cuyo paciente, dentista o servicio
           // está eliminado. La fase del expediente debe hacer lo mismo en sus consultas.
           .ConfigureWarnings(w => w.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

// Inyección de dependencias
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<ICitaHistorialService, CitaHistorialService>();
builder.Services.AddScoped<DatosInicialesService>();
builder.Services.AddScoped<SembradoDemoService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICitasRepository, CitasRepository>();
builder.Services.AddScoped<IAgendaService, AgendaService>();
builder.Services.AddScoped<backend.Services.Agenda.ICalendarioAtencion, backend.Services.Agenda.CalendarioAtencion>();
builder.Services.AddScoped<backend.Services.Agenda.IDisponibilidadService, backend.Services.Agenda.DisponibilidadService>();
builder.Services.AddScoped<backend.Services.Agenda.IReservasService, backend.Services.Agenda.ReservasService>();
builder.Services.AddScoped<backend.Services.Agenda.ICitasEstadoService, backend.Services.Agenda.CitasEstadoService>();
builder.Services.AddScoped<backend.Services.Agenda.IConfiguracionAgendaService, backend.Services.Agenda.ConfiguracionAgendaService>();
builder.Services.AddScoped<backend.Services.Agenda.ISolicitudesPublicasService, backend.Services.Agenda.SolicitudesPublicasService>();
builder.Services.AddScoped<backend.Services.Pacientes.IPacientesPanelService, backend.Services.Pacientes.PacientesPanelService>();
builder.Services.AddSingleton<backend.Services.Agenda.ITokensCitaService, backend.Services.Agenda.TokensCitaService>();

// Correo (diseño de la agenda, sección 7): bandeja de salida y envío por SMTP (Mailpit en desarrollo).
// Sin valores en el repositorio: user-secrets o variables de entorno (docs/correo/desarrollo.md).
builder.Services.Configure<backend.Services.Correo.OpcionesCorreo>(builder.Configuration.GetSection("Correo"));
builder.Services.Configure<backend.Services.Correo.OpcionesClinica>(builder.Configuration.GetSection("Clinica"));
builder.Services.AddSingleton<backend.Services.Correo.IEmailSender, backend.Services.Correo.SmtpEmailSender>();
builder.Services.AddSingleton<backend.Services.Correo.AvisoBandeja>();
builder.Services.AddSingleton<backend.Services.Correo.IProcesadorBandeja, backend.Services.Correo.ProcesadorBandeja>();
builder.Services.AddScoped<backend.Services.Correo.IRecordatorios, backend.Services.Correo.Recordatorios>();
if (builder.Configuration.GetValue<bool>("Correo:Habilitado"))
{
    if (string.IsNullOrWhiteSpace(builder.Configuration["Correo:Smtp:Host"]) || string.IsNullOrWhiteSpace(builder.Configuration["Correo:Remitente"]))
        throw new InvalidOperationException("Correo:Habilitado requiere Correo:Smtp:Host y Correo:Remitente (user-secrets o variables de entorno).");
    builder.Services.AddHostedService<backend.Services.Correo.EnvioCorreosService>();
    builder.Services.AddHostedService<backend.Services.Correo.RecordatoriosService>();
}
builder.Services.AddScoped<IPacientesService, PacientesService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRelojClinica, RelojClinica>();
builder.Services.AddSingleton<ICatalogoImagenesService, CatalogoImagenesService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// HS256 requiere una clave de al menos 256 bits (32 bytes)
if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
    throw new InvalidOperationException(
        "JwtSettings:SecretKey falta o es demasiado corta (mínimo 32 bytes). Configúrala con user-secrets o con la variable de entorno JwtSettings__SecretKey.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // sin margen extra de tiempo
    };
});

// Todo endpoint exige sesión salvo que se marque [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// CORS para React: solo los orígenes de Cors:AllowedOrigins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Límites de peticiones para login y registro
builder.Services.AddDentissaRateLimiting(builder.Configuration);

builder.Services.AddControllers();
// Errores 500 como ProblemDetails genérico: sin pila ni mensajes internos fuera de desarrollo
builder.Services.AddProblemDetails();

// Detrás de un proxy (Render; Vercel → Render) la IP real llega en X-Forwarded-For. Proxy:Saltos indica
// cuántos proxies se confían (0 = ninguno, en local); de ella dependen el límite de intentos y la bitácora.
var saltosProxy = builder.Configuration.GetValue<int>("Proxy:Saltos");
if (saltosProxy > 0)
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        o.ForwardLimit = saltosProxy;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Configurar Swagger para enviar JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Escribe: Bearer {tu token}"
    });
    // Microsoft.OpenApi 2.x: la referencia al esquema se resuelve contra el documento
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Migraciones (solo si se pide), configuración de agenda y administrador inicial
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DatosInicialesService>().EjecutarAsync();

// Demo (docs/DEMO.md): "--sembrar-demo" siembra datos ficticios en una base vacía, escribe las cuentas del
// personal una sola vez en la salida estándar y termina sin levantar la API.
if (args.Contains("--sembrar-demo"))
{
    using var scope = app.Services.CreateScope();
    IReadOnlyList<SembradoDemoService.Cuenta> cuentas;
    try
    {
        cuentas = await scope.ServiceProvider.GetRequiredService<SembradoDemoService>().EjecutarAsync();
    }
    catch (InvalidOperationException e)
    {
        // Base con datos o demo no habilitada: no se toca nada
        Console.Error.WriteLine(e.Message);
        Environment.ExitCode = 3;
        return;
    }
    Console.WriteLine();
    Console.WriteLine("==== Dentissa: cuentas de la DEMO (datos ficticios; contraseñas generadas ahora) ====");
    foreach (var c in cuentas)
        Console.WriteLine($"  {c.Rol,-10} {c.Nombre,-16} {c.Correo,-36} {c.Password}");
    Console.WriteLine("Guárdalas ahora: no se vuelven a mostrar ni quedan en el repositorio.");
    Console.WriteLine();
    return;
}

// Despliegue (docs/DESPLIEGUE.md): con DEMO_SEMBRAR=true siembra los datos ficticios si la base está vacía,
// con las contraseñas de DEMO_PASSWORD_ADMIN y DEMO_PASSWORD_PERSONAL. Nunca las escribe en los logs.
if (app.Configuration.GetValue<bool>("DEMO_SEMBRAR") && !args.Contains("--sembrar-demo"))
{
    using var scope = app.Services.CreateScope();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<SembradoDemoService>>();
    var sembrado = scope.ServiceProvider.GetRequiredService<SembradoDemoService>();
    var admin = app.Configuration["DEMO_PASSWORD_ADMIN"];
    var personal = app.Configuration["DEMO_PASSWORD_PERSONAL"];
    if ((admin?.Length ?? 0) < DatosInicialesService.LongitudMinimaPassword || (personal?.Length ?? 0) < DatosInicialesService.LongitudMinimaPassword)
        log.LogWarning("DEMO_SEMBRAR está activo, pero faltan DEMO_PASSWORD_ADMIN o DEMO_PASSWORD_PERSONAL (mínimo {Minimo} caracteres): no se sembró.",
            DatosInicialesService.LongitudMinimaPassword);
    else if (!await sembrado.BaseVaciaAsync())
        log.LogInformation("DEMO_SEMBRAR está activo, pero la base ya tiene datos: no se siembra de nuevo.");
    else
    {
        await sembrado.EjecutarAsync(new SembradoDemoService.ContrasenasFijas(admin!, personal!));
        log.LogInformation("Se sembraron los datos de la demo.");
    }
}

if (saltosProxy > 0)
    app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // En desarrollo el proxy de Vite habla HTTP con la API
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("ReactPolicy");
app.UseLoginCorreoParaRateLimit();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Para el chequeo de salud de Render: sin sesión, sin datos; 503 si no hay base
app.MapGet("/health", async (DentissaDbContext db, CancellationToken ct) =>
        await db.Database.CanConnectAsync(ct) ? Results.Ok(new { estado = "ok" }) : Results.StatusCode(503))
    .AllowAnonymous();
app.Run();

// Permite que las pruebas de integración usen WebApplicationFactory<Program>
public partial class Program { }
