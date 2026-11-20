using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReservaEspacios.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada solo por `dotnet ef`. La cadena se lee de la variable de entorno
/// ConnectionStrings__ReservaEspacios para no versionar credenciales (ADR-003).
/// </summary>
public sealed class ReservaEspaciosDbContextFactory : IDesignTimeDbContextFactory<ReservaEspaciosDbContext>
{
    public ReservaEspaciosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ReservaEspacios")
            ?? throw new InvalidOperationException(
                "Defina ConnectionStrings__ReservaEspacios antes de ejecutar dotnet ef.");

        var options = new DbContextOptionsBuilder<ReservaEspaciosDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ReservaEspaciosDbContext(options);
    }
}
