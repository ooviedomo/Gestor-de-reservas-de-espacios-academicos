using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Decorator (ADR-002): traduce el error SQL crudo de la restricción de exclusión
/// —y un fallo de serialización— al código `SOLAPE_DETECTADO` definido en Application,
/// antes de que llegue a la API. La transacción aplica la misma traducción al confirmar.
/// </summary>
public sealed class TraductorErroresReservaDecorator(IReservaRepository inner) : IReservaRepository
{
    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        try
        {
            await inner.AddAsync(reserva, cancellationToken);
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
