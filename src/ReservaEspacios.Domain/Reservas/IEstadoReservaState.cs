namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// State (ADR-002): comportamiento asociado al estado actual de la reserva.
/// Cada estado concreto decide qué acciones acepta y cómo se registran.
/// </summary>
internal interface IEstadoReservaState
{
    EstadoReserva Estado { get; }

    void Aprobar(Reservation reserva, SolicitudTransicion solicitud);

    void Rechazar(Reservation reserva, SolicitudTransicion solicitud);

    void Cancelar(Reservation reserva, SolicitudTransicion solicitud);

    void Completar(Reservation reserva, SolicitudTransicion solicitud);
}
