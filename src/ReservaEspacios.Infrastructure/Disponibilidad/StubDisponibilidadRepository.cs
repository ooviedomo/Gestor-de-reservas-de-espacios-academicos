using ReservaEspacios.Application.Disponibilidad;

namespace ReservaEspacios.Infrastructure.Disponibilidad;

/// <summary>
/// Stub deliberado: mientras no exista la tabla `reservations`, ninguna reserva bloquea.
/// El siguiente change sustituye este registro por un repositorio EF Core real sin
/// tocar `ConsultarDisponibilidad` ni sus pruebas.
/// </summary>
public sealed class StubDisponibilidadRepository : IDisponibilidadRepository
{
    public Task<IReadOnlyList<ReservaBloqueante>> ObtenerReservasAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReservaBloqueante>>(Array.Empty<ReservaBloqueante>());
}
