namespace backend.Security
{
    // Cabeceras de seguridad básicas para todas las respuestas de la API
    public static class SecurityHeadersMiddleware
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";

                // La API no sirve páginas; Swagger (solo en desarrollo) necesita scripts y estilos
                if (!context.Request.Path.StartsWithSegments("/swagger"))
                    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

                await next();
            });
        }
    }
}
