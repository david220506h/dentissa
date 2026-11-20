using backend.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace backend.Tests;

// Un solo contenedor de PostgreSQL 17 para toda la ejecución de pruebas.
// La migración se aplica una vez en una base plantilla y cada fábrica recibe una base
// propia copiada de ella (CREATE DATABASE … TEMPLATE), así las pruebas no se pisan.
// Testcontainers (Ryuk) elimina el contenedor al terminar el proceso.
public static class PostgresCompartido
{
    public const string Imagen = "postgres:17-alpine";
    private const string Plantilla = "dentissa_plantilla";

    private static readonly SemaphoreSlim Candado = new(1, 1);
    private static PostgreSqlContainer? _contenedor;
    private static bool _plantillaLista;

    // Base vacía, sin migraciones: para probar que la migración se aplica desde cero
    public static async Task<string> CrearBaseVaciaAsync()
    {
        await IniciarAsync();
        return await CrearBaseAsync(plantilla: null);
    }

    // Base con la migración Inicial ya aplicada
    public static async Task<string> CrearBaseMigradaAsync()
    {
        await IniciarAsync();
        return await CrearBaseAsync(Plantilla);
    }

    private static async Task IniciarAsync()
    {
        if (_plantillaLista) return;
        await Candado.WaitAsync();
        try
        {
            if (_plantillaLista) return;

            _contenedor ??= new PostgreSqlBuilder(Imagen)
                .Build();
            await _contenedor.StartAsync();

            await EjecutarAsync($"CREATE DATABASE {Plantilla}");
            var opciones = new DbContextOptionsBuilder<DentissaDbContext>()
                .UseNpgsql(Cadena(Plantilla))
                .UseSnakeCaseNamingConvention()
                .Options;
            await using (var db = new DentissaDbContext(opciones))
                await db.Database.MigrateAsync();

            // CREATE DATABASE … TEMPLATE exige que nadie esté conectado a la plantilla
            NpgsqlConnection.ClearAllPools();
            _plantillaLista = true;
        }
        finally
        {
            Candado.Release();
        }
    }

    private static async Task<string> CrearBaseAsync(string? plantilla)
    {
        var nombre = "dentissa_" + Guid.NewGuid().ToString("N");
        await EjecutarAsync(plantilla == null
            ? $"CREATE DATABASE {nombre}"
            : $"CREATE DATABASE {nombre} TEMPLATE {plantilla}");
        return Cadena(nombre);
    }

    private static string Cadena(string baseDatos)
        => new NpgsqlConnectionStringBuilder(_contenedor!.GetConnectionString()) { Database = baseDatos }.ConnectionString;

    // Las copias desde la plantilla se serializan: PostgreSQL no permite dos a la vez
    private static readonly SemaphoreSlim CandadoCopias = new(1, 1);

    private static async Task EjecutarAsync(string sql)
    {
        await CandadoCopias.WaitAsync();
        try
        {
            await using var conexion = new NpgsqlConnection(Cadena("postgres"));
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(sql, conexion);
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            CandadoCopias.Release();
        }
    }
}
