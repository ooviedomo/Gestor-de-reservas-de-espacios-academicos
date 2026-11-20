using System.Data;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Transacción SERIALIZABLE (tareas 3.2/3.3). La revalidación y la inserción comparten
/// el DbContext del scope, de modo que la consulta ve el mismo snapshot que la escritura.
/// La restricción <c>no_overlap</c> sigue siendo la última barrera (RES-002).
/// PostgreSQL puede abortar una de dos transacciones concurrentes con <c>40001</c> al
/// confirmar; se traduce aquí porque el decorator del repositorio solo cubre
/// <c>AddAsync</c>. Al lanzar, el <c>await using</c> hace rollback automático.
/// </summary>
public sealed class TransaccionReserva(ReservaEspaciosDbContext context) : ITransaccionReserva
{
    public async Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        try
        {
            await operacion(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var traducido = TraductorPostgresReserva.Traducir(exception);
            if (traducido is not null)
            {
                throw traducido;
            }

            throw;
        }
    }
}
