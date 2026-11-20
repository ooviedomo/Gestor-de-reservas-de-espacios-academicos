using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Decorator (ADR-002): toda mutación persistida de una reserva invalida las franjas
/// cacheadas de su espacio. Crear, cancelar, aprobar o completar una reserva cambia
/// la disponibilidad de ese espacio (tarea 3.1 y 3.4). Se registra por fuera del
/// traductor de errores: si la escritura falla, no invalida.
/// </summary>
public sealed class InvalidarCacheReservaDecorator(
    IReservaRepository inner,
    IDisponibilidadCache cache) : IReservaRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Reservation>> ObtenerAprobadasVencidasAsync(
        DateTimeOffset ahora, CancellationToken cancellationToken = default) =>
        inner.ObtenerAprobadasVencidasAsync(ahora, cancellationToken);

    public Task<IReadOnlyList<Reservation>> ObtenerHistorialAsync(
        FiltroHistorial filtro, CancellationToken cancellationToken = default) =>
        inner.ObtenerHistorialAsync(filtro, cancellationToken);

    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        await inner.AddAsync(reserva, cancellationToken);
        cache.Invalidar(reserva.SpaceId);
    }

    public async Task UpdateAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        await inner.UpdateAsync(reserva, cancellationToken);
        cache.Invalidar(reserva.SpaceId);
    }
}
