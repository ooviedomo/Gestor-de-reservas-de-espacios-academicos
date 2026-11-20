using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>Persistencia EF de reservas; la restricción de base de datos se deja actuar.</summary>
public sealed class ReservaRepository(ReservaEspaciosDbContext context) : IReservaRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> ObtenerAprobadasVencidasAsync(
        DateTimeOffset ahora, CancellationToken cancellationToken = default)
    {
        var limite = ahora.ToUniversalTime();
        return await context.Reservations
            .Where(r => r.Estado == EstadoReserva.Aprobada && r.Fin <= limite)
            .OrderBy(r => r.Fin)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        context.Reservations.Add(reserva);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        // La entidad ya está trackeada por GetByIdAsync; Update la marca como modificada
        // también si el caso de uso se ejecutara con un DbContext nuevo (tarea 3.4).
        context.Reservations.Update(reserva);
        await context.SaveChangesAsync(cancellationToken);
    }
}
