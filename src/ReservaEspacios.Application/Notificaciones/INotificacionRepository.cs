namespace ReservaEspacios.Application.Notificaciones;

/// <summary>
/// Outbox de correo (NOT-001/002/003). Application solo conoce esta abstracción: la
/// inserción la hacen los suscriptores dentro de la transacción del caso de uso y el
/// worker actualiza estado e intentos después, sin revertir la reserva.
/// </summary>
public interface INotificacionRepository
{
    Task AgregarAsync(NotificacionPendienteDto notificacion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificacionPendienteDto>> ObtenerPendientesAsync(
        DateTimeOffset ahora,
        int limite,
        CancellationToken cancellationToken = default);

    Task MarcarEnviadaAsync(
        Guid id, DateTimeOffset enviadoEn, CancellationToken cancellationToken = default);

    Task RegistrarFalloAsync(
        Guid id,
        int intentos,
        string error,
        DateTimeOffset proximoIntento,
        bool agotado,
        CancellationToken cancellationToken = default);
}
