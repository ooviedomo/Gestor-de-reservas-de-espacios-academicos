using Microsoft.Extensions.Options;
using ReservaEspacios.Application.Notificaciones;

namespace ReservaEspacios.Web.BackgroundServices;

/// <summary>
/// IHostedService (NOT-002/003): extrae el outbox en un proceso independiente de la
/// transacción de reserva. Si el proveedor falla, el procesador lo registra en la fila y
/// el worker continúa; nunca detiene el host ni revierte la reserva.
/// </summary>
public sealed class NotificacionesWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificacionesOptions> options,
    ILogger<NotificacionesWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervaloSegundos));
        using var timer = new PeriodicTimer(intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var procesador = scope.ServiceProvider
                    .GetRequiredService<IProcesadorNotificaciones>();
                var procesadas = await procesador.ProcesarAsync(
                    options.Value.Lote, cancellationToken: stoppingToken);

                if (procesadas > 0)
                {
                    logger.LogInformation(
                        "Notificaciones procesadas: {Procesadas}.", procesadas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error al procesar el outbox de notificaciones.");
            }
        }
    }
}
