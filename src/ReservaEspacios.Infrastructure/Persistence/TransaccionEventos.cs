using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Infrastructure.Persistence;

/// <summary>
/// Adapter de <see cref="ITransaccionEventos"/> sobre la transacción del DbContext del
/// scope. EF Core inscribe todos los <c>SaveChangesAsync</c> realizados dentro del
/// delegado en esta transacción: la reserva, su registro de auditoría y su outbox se
/// confirman o se revierten juntos. Si el delegado lanza, <c>await using</c> hace rollback.
/// </summary>
public sealed class TransaccionEventos(ReservaEspaciosDbContext context) : ITransaccionEventos
{
    public async Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaccion =
            await context.Database.BeginTransactionAsync(cancellationToken);

        await operacion(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }
}
