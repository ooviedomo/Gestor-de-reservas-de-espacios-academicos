namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// Puerto (ADR-002) que aísla el caso de uso de la persistencia de reservas.
/// Devuelve las reservas que solapan la franja; la regla de qué estados bloquean
/// permanece en <see cref="ConsultarDisponibilidad"/>, que la delega en Domain.
/// </summary>
public interface IDisponibilidadRepository
{
    Task<IReadOnlyList<ReservaBloqueante>> ObtenerReservasAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default);
}
