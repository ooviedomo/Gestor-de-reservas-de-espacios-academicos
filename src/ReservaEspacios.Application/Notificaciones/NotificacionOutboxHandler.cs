using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Notificaciones;

/// <summary>
/// Observer (ADR-002): reacciona solo a las transiciones de NOT-001 —aprobación, rechazo
/// y cancelación—. La creación en `PENDIENTE` y el completado no generan correo. Escribe
/// el outbox con el mismo DbContext del caso de uso, dentro de `ITransaccionEventos`.
/// </summary>
public sealed class NotificacionOutboxHandler(
    IReservaRepository reservas,
    ISpaceRepository espacios,
    IUsuarioIdentityService identity,
    INotificacionRepository notificaciones) : IDomainEventSubscriber
{
    public async Task HandleAsync(
        IDomainEvent evento, CancellationToken cancellationToken = default)
    {
        var (reservaId, espacioId, solicitanteId, asunto, detalle) = evento switch
        {
            ReservaAprobada aprobada => (
                aprobada.ReservaId, aprobada.EspacioId, aprobada.SolicitanteId,
                "Reserva aprobada", string.Empty),
            ReservaRechazada rechazada => (
                rechazada.ReservaId, rechazada.EspacioId, rechazada.SolicitanteId,
                "Reserva rechazada", $" Motivo: {rechazada.Motivo}."),
            ReservaCancelada cancelada => (
                cancelada.ReservaId, cancelada.EspacioId, cancelada.SolicitanteId,
                "Reserva cancelada", $" Motivo: {cancelada.Motivo}."),
            _ => (Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, string.Empty)
        };

        if (reservaId == Guid.Empty)
        {
            return;
        }

        var destinatario = await identity.ObtenerPorIdAsync(solicitanteId, cancellationToken);
        if (destinatario is null || !destinatario.IsSuccess ||
            destinatario.Value is not { Email.Length: > 0 } solicitante)
        {
            // Sin destinatario no existe mensaje válido. No lanzar: un fallo aquí
            // revertiría la transición de la reserva, lo que NOT-003 prohíbe.
            return;
        }

        var reserva = await reservas.GetByIdAsync(reservaId, cancellationToken);
        var espacio = await espacios.GetByIdAsync(espacioId, cancellationToken);
        var nombreEspacio = espacio?.Nombre ?? "el espacio solicitado";
        var franja = reserva is null
            ? string.Empty
            : $" El {reserva.Inicio:dd/MM/yyyy} de {reserva.Inicio:HH:mm} a {reserva.Fin:HH:mm}.";

        var cuerpo = $"Estimado/a {solicitante.Nombre}: su reserva en {nombreEspacio} " +
            $"ha cambiado de estado: {asunto}.{franja}{detalle}";

        await notificaciones.AgregarAsync(
            new NotificacionPendienteDto(
                Guid.NewGuid(), reservaId, solicitante.Email, asunto, cuerpo, Intentos: 0),
            cancellationToken);
    }
}
