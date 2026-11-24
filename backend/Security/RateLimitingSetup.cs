using System.Text.Json;
using System.Threading.RateLimiting;

namespace backend.Security
{
    // Límites de peticiones para login y registro (valores en RateLimiting:* de la configuración).
    //   Login:    por IP + correo y, además, un tope general por IP.
    //   Registro: por IP.
    //   Público (/api/publico/*): por IP (disponibilidad y, en la Fase 2, solicitudes y enlaces).
    public static class RateLimitingSetup
    {
        private const string LoginPath    = "/api/auth/login";
        private const string RegistroPath = "/api/auth/register";
        private const string CorreoItem   = "rate-limit:correo";

        // Solo se lee el cuerpo del login si es pequeño
        private const long MaxCuerpoLogin = 4096;

        public static IServiceCollection AddDentissaRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            var loginIpCorreo = LeerVentana(config, "RateLimiting:Login:PorIpYCorreo", 5, 60);
            var loginIp       = LeerVentana(config, "RateLimiting:Login:PorIp", 30, 60);
            var registroIp    = LeerVentana(config, "RateLimiting:Registro:PorIp", 3, 600);
        var publicoIp     = LeerVentana(config, "RateLimiting:Publico:PorIp", 60, 60);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { mensaje = "Demasiados intentos. Espera un momento e inténtalo de nuevo." }, token);
                };

                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    Particion(EsLogin,    ctx => $"login-ip:{Ip(ctx)}", loginIp),
                    Particion(EsLogin,    ctx => $"login-ip-correo:{Ip(ctx)}|{Correo(ctx)}", loginIpCorreo),
                    Particion(EsRegistro, ctx => $"registro-ip:{Ip(ctx)}", registroIp),
                    Particion(EsPublico,  ctx => $"publico-ip:{Ip(ctx)}", publicoIp));
            });

            return services;
        }

        // Lee el correo del cuerpo del login (antes del limitador) para la partición IP + correo
        public static IApplicationBuilder UseLoginCorreoParaRateLimit(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (EsLogin(context) && context.Request.ContentLength is > 0 and <= MaxCuerpoLogin)
                {
                    context.Request.EnableBuffering();
                    try
                    {
                        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                if (prop.Name.Equals("email", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                        context.Items[CorreoItem] = prop.Value.GetString()!.Trim().ToLowerInvariant();
                                    break;
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Cuerpo inválido: se limita solo por IP y el controlador responde 400
                    }
                    context.Request.Body.Position = 0;
                }

                await next();
            });
        }

        private static PartitionedRateLimiter<HttpContext> Particion(
            Func<HttpContext, bool> aplica, Func<HttpContext, string> clave, FixedWindowRateLimiterOptions opciones)
        {
            return PartitionedRateLimiter.Create<HttpContext, string>(ctx => aplica(ctx)
                ? RateLimitPartition.GetFixedWindowLimiter(clave(ctx), _ => opciones)
                : RateLimitPartition.GetNoLimiter("sin-limite"));
        }

        private static FixedWindowRateLimiterOptions LeerVentana(IConfiguration config, string seccion, int permisos, int segundos)
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue<int?>($"{seccion}:PermitLimit") ?? permisos,
                Window = TimeSpan.FromSeconds(config.GetValue<int?>($"{seccion}:WindowSeconds") ?? segundos),
                QueueLimit = 0,
                AutoReplenishment = true
            };
        }

        private static bool EsLogin(HttpContext ctx) => EsPost(ctx, LoginPath);
        private static bool EsRegistro(HttpContext ctx) => EsPost(ctx, RegistroPath);
        private static bool EsPublico(HttpContext ctx) =>
            ctx.Request.Path.StartsWithSegments("/api/publico", StringComparison.OrdinalIgnoreCase);

        private static bool EsPost(HttpContext ctx, string path) =>
            HttpMethods.IsPost(ctx.Request.Method) &&
            ctx.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase);

        private static string Ip(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        private static string Correo(HttpContext ctx) => ctx.Items[CorreoItem] as string ?? "";
    }
}
