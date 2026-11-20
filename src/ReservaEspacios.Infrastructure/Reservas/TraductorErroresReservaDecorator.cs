using Npgsql;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Decorator (ADR-002): traduce el error SQL crudo de la restricción de exclusión
/// al código `SOLAPE_DETECTADO` definido en Application, antes de que llegue a la API.
/// </summary>
public sealed class TraductorErroresReservaDecorator(IReservaRepository inner) : IReservaRepository
{
    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        try
        {
            await inner.AddAsync(reserva, cancellationToken);
        }
        catch (Exception ex)
        {
            // EF Core envuelve la PostgresException en DbUpdateException al llamar a SaveChanges.
            var postgres = ex as PostgresException ?? ex.InnerException as PostgresException;
            var traducido = postgres is null
                ? null
                : TraductorErroresReserva.Traducir(postgres.SqlState, postgres.ConstraintName);
            if (traducido is null)
            {
                throw;
            }

            throw traducido;
        }
    }
}
