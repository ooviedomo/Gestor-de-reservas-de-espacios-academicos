using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>Persistencia EF de reservas; la restricción de base de datos se deja actuar.</summary>
public sealed class ReservaRepository(ReservaEspaciosDbContext context) : IReservaRepository
{
    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        context.Reservations.Add(reserva);
        await context.SaveChangesAsync(cancellationToken);
    }
}
