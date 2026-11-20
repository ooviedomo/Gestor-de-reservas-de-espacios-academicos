using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReservaEspacios.Infrastructure.Persistence;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Aísla cada clase de prueba en un esquema propio de PostgreSQL local (ADR-003).
/// Sustituye el contenedor de Testcontainers sin perder el aislamiento entre pruebas.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim ExtensionLock = new(1, 1);
    private readonly string _adminConnectionString;

    public string SchemaName { get; } = $"test_{Guid.NewGuid():N}";
    public string ConnectionString { get; }
    public DbContextOptions<ReservaEspaciosDbContext> Options { get; }

    public PostgresFixture()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ReservaEspaciosTest")
            ?? throw new InvalidOperationException(
                "Defina ConnectionStrings__ReservaEspaciosTest antes de ejecutar las pruebas de integración.");

        ConnectionString = $"{_adminConnectionString.TrimEnd(';')};Search Path={SchemaName}";
        Options = new DbContextOptionsBuilder<ReservaEspaciosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public ReservaEspaciosDbContext CreateContext() => new(Options);

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        // Las extensiones son de base de datos, no de esquema: se crea una sola vez en
        // public para que las migraciones de cada esquema de prueba no compitan (ADR-003).
        await ExtensionLock.WaitAsync();
        try
        {
            await ExecuteAsync(connection, "CREATE EXTENSION IF NOT EXISTS btree_gist WITH SCHEMA public;");
        }
        finally
        {
            ExtensionLock.Release();
        }

        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE");
        await ExecuteAsync(connection, $"CREATE SCHEMA \"{SchemaName}\"");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
