using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Decorator (ADR-002): toda mutación persistida de una reserva invalida las franjas
/// cacheadas de su espacio, de modo que `ConsultarDisponibilidad` no devuelva una
/// franja libre después de crear, cancelar, aprobar o completar una reserva (tarea 3.1).
/// Se registra por fuera del traductor de errores: si la escritura falla, no invalida.
/// </summary>
public sealed class InvalidarCacheReservaDecorator(
    IReservaRepository inner,
    IDisponibilidadCache cache) : IReservaRepository
{
    public async Task AddAsync(Reservation reserva, CancellationToken cancellationToken = default)
    {
        await inner.AddAsync(reserva, cancellationToken);
        cache.Invalidar(reserva.SpaceId);
    }
}
