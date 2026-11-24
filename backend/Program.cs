using System.Text;
using backend.Entities;
using backend.Repositories;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Los secretos vienen de user-secrets (desarrollo) o variables de entorno (producción).
// Si faltan, la aplicación no arranca.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICitasRepository, CitasRepository>();
builder.Services.AddScoped<IAgendaService, AgendaService>();
builder.Services.AddScoped<backend.Services.Agenda.ICalendarioAtencion, backend.Services.Agenda.CalendarioAtencion>();
builder.Services.AddScoped<backend.Services.Agenda.IDisponibilidadService, backend.Services.Agenda.DisponibilidadService>();
builder.Services.AddScoped<backend.Services.Agenda.IReservasService, backend.Services.Agenda.ReservasService>();
builder.Services.AddScoped<backend.Services.Agenda.ICitasEstadoService, backend.Services.Agenda.CitasEstadoService>();
builder.Services.AddSingleton<backend.Services.Agenda.ITokensCitaService, backend.Services.Agenda.TokensCitaService>();
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
app.Run();

// Permite que las pruebas de integración usen WebApplicationFactory<Program>
public partial class Program { }
