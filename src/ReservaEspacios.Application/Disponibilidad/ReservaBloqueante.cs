namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// Proyección mínima de una reserva para evaluar disponibilidad. La tabla `reservations`
/// se creará en el siguiente change; hasta entonces este tipo la simula (design.md).
/// </summary>
public sealed record ReservaBloqueante(
    Guid Id,
    Guid SpaceId,
    string Estado,
    DateTimeOffset Inicio,
    DateTimeOffset Fin);
