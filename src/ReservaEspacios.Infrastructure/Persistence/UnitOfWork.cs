using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Infrastructure.Persistence;

public sealed class UnitOfWork(ReservaEspaciosDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: "23505" })
        {
            // Última línea de defensa: carrera entre dos altas con el mismo nombre.
            throw new DomainException(
                DomainErrors.NombreDuplicado,
                "Ya existe un espacio con ese nombre.",
                exception);
        }
    }
}
