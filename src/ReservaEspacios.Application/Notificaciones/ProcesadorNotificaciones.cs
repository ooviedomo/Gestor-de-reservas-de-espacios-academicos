using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Notificaciones;

/// <summary>
/// Caso de uso invocado por el worker: envía el outbox pendiente y actualiza estado,
/// intentos y último error. Nunca lanza hacia la transición de reserva: los fallos del
/// proveedor se convierten en datos de la fila (NOT-002/NOT-003).
/// </summary>
public sealed class ProcesadorNotificaciones(
    INotificacionRepository repositorio,
    ICommandHandler<EnviarCorreoCommand> enviar,
    IRetryBackoff backoff) : IProcesadorNotificaciones
{
    public async Task<int> ProcesarAsync(
        int limite = 20,
        DateTimeOffset? ahora = null,
        CancellationToken cancellationToken = default)
    {
        var momento = ahora ?? DateTimeOffset.UtcNow;
        var pendientes = await repositorio.ObtenerPendientesAsync(
            momento, Math.Max(1, limite), cancellationToken);
        var procesadas = 0;

        foreach (var notificacion in pendientes)
        {
            var resultado = await enviar.HandleAsync(
                new EnviarCorreoCommand(
                    notificacion.Destinatario, notificacion.Asunto, notificacion.Cuerpo),
                cancellationToken);

            if (resultado.IsSuccess)
            {
                await repositorio.MarcarEnviadaAsync(notificacion.Id, momento, cancellationToken);
            }
            else
            {
                var intentos = notificacion.Intentos + 1;
                await repositorio.RegistrarFalloAsync(
                    notificacion.Id,
                    intentos,
                    resultado.ErrorCode ?? "CORREO_NO_ENVIADO",
                    momento + backoff.Calcular(intentos),
                    intentos >= backoff.MaxIntentos,
                    cancellationToken);
            }

            procesadas++;
        }

        return procesadas;
    }
}

/// <summary>Abstracción para que el worker de Web no dependa del manejador concreto.</summary>
public interface IProcesadorNotificaciones
{
    Task<int> ProcesarAsync(
        int limite = 20,
        DateTimeOffset? ahora = null,
        CancellationToken cancellationToken = default);
}
