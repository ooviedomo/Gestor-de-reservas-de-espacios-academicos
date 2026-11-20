using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

/// <summary>Escritura de reservas. La traducción de errores se aplica como Decorator.</summary>
public interface IReservaRepository
{
    Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default);
}
