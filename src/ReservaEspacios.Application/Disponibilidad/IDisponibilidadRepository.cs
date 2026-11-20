namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// Aísla la consulta de disponibilidad de la persistencia de reservas, que todavía
/// no existe en este change (ADR-002: puerto + stub).
/// </summary>
public interface IDisponibilidadRepository
{
    Task<IReadOnlyList<ReservaBloqueante>> ObtenerReservasAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default);
}
