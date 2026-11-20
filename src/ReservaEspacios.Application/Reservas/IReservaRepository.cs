using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// Persistencia de reservas. La traducción de errores y la invalidación de la caché
/// de disponibilidad se aplican como Decorators sobre todas las mutaciones.
/// </summary>
public interface IReservaRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>COM-001: reservas `APROBADA` cuyo `fin` ya venció, para el cierre automático.</summary>
    Task<IReadOnlyList<Reservation>> ObtenerAprobadasVencidasAsync(
        DateTimeOffset ahora, CancellationToken cancellationToken = default);

    /// <summary>RES-004: historial con filtros; la visibilidad por rol se resuelve en Application.</summary>
    Task<IReadOnlyList<Reservation>> ObtenerHistorialAsync(
        FiltroHistorial filtro, CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reserva, CancellationToken cancellationToken = default);
}
