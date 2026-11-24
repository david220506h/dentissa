using Npgsql;

namespace backend.Services
{
    /// <summary>
    /// Acepta la cadena de Npgsql ("Host=…;Database=…") o la URI de Neon
    /// ("postgresql://usuario:clave@host/base?sslmode=require") y devuelve siempre la de Npgsql.
    /// Con la URI exige SSL (sslmode=require por defecto).
    /// </summary>
    public static class CadenaConexion
    {
        public static string? Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return valor;
            var v = valor.Trim();
            if (!v.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !v.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                return v;

            var uri = new Uri(v);
            var credenciales = uri.UserInfo.Split(':', 2);
            var b = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(credenciales[0]),
                Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null,
                SslMode = SslMode.Require,
            };
            foreach (var par in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = par.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<SslMode>(kv[1].Replace("-", ""), true, out var modo) && modo >= SslMode.Require)
                    b.SslMode = modo;
            }
            return b.ConnectionString;
        }
    }
}
