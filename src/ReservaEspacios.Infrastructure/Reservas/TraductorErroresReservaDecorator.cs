using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Decorator (ADR-002): traduce el error SQL crudo —restricción de exclusión o fallo
/// de serialización— al código `SOLAPE_DETECTADO` definido en Application, antes de
/// que llegue a la API. Cubre alta y actualización; la transacción aplica la misma
/// traducción al confirmar.
/// </summary>
public sealed class TraductorErroresReservaDecorator(IReservaRepository inner) : IReservaRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default) =>
        EjecutarAsync(() => inner.AddAsync(reserva, cancellationToken));

    public Task UpdateAsync(Reservation reserva, CancellationToken cancellationToken = default) =>
        EjecutarAsync(() => inner.UpdateAsync(reserva, cancellationToken));

    private static async Task EjecutarAsync(Func<Task> operacion)
    {
        try
        {
            await operacion();
        }
        catch (Exception exception)
        {
            var traducido = TraductorPostgresReserva.Traducir(exception);
            if (traducido is null)
            {
                throw;
            }

            throw traducido;
        }
    }
}
