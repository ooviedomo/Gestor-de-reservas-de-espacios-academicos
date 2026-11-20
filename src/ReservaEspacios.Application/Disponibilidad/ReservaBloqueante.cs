namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// Proyección mínima de una reserva persistida para evaluar disponibilidad (tarea 3.1).
/// <c>Estado</c> es el código canónico de <c>EstadoReserva</c>, p. ej. <c>PENDIENTE</c>.
/// </summary>
public sealed record ReservaBloqueante(
    Guid Id,
    Guid SpaceId,
    string Estado,
    DateTimeOffset Inicio,
    DateTimeOffset Fin);
