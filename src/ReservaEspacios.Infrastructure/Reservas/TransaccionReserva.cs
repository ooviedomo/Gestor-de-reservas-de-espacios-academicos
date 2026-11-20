using System.Data;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Transacción SERIALIZABLE (tarea 3.2). La revalidación y la inserción comparten el
/// DbContext del scope, de modo que la consulta ve el mismo snapshot que la escritura.
/// La restricción `no_overlap` sigue siendo la última barrera (RES-002).
/// </summary>
public sealed class TransaccionReserva(ReservaEspaciosDbContext context) : ITransaccionReserva
{
    public async Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        await operacion(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }
}
