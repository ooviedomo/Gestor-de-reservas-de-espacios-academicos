using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Disponibilidad;

/// <summary>
/// Tarea 3.1: implementación EF real que sustituye al stub sobre `reservations`.
/// Filtra por espacio e intervalo semiabierto, pero no por estado: la decisión de qué
/// reservas bloquean sigue viviendo en <see cref="ConsultarDisponibilidad"/>, que la
/// delega en Domain (DISP-002, una sola fuente de verdad).
/// </summary>
public sealed class DisponibilidadRepository(ReservaEspaciosDbContext context)
    : IDisponibilidadRepository
{
    public async Task<IReadOnlyList<ReservaBloqueante>> ObtenerReservasAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default)
    {
        var inicioUtc = inicio.ToUniversalTime();
        var finUtc = fin.ToUniversalTime();

        var reservas = await context.Reservations
            .AsNoTracking()
            .Where(r => r.SpaceId == spaceId && r.Inicio < finUtc && inicioUtc < r.Fin)
            .ToListAsync(cancellationToken);

        return reservas
            .Select(r => new ReservaBloqueante(r.Id, r.SpaceId, r.Estado.Codigo(), r.Inicio, r.Fin))
            .ToArray();
    }
}
