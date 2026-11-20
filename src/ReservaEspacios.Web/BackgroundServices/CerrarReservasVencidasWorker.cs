using Microsoft.Extensions.Options;
using ReservaEspacios.Application.Reservas;

namespace ReservaEspacios.Web.BackgroundServices;

/// <summary>
/// Hosted service (COM-001): dispara periódicamente el caso de uso de cierre automático.
/// No contiene reglas de dominio: solo resuelve un scope por tick, invoca
/// <see cref="ICerrarReservasVencidas"/> y registra errores sin detener el host.
/// </summary>
public sealed class CerrarReservasVencidasWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CierreAutomaticoOptions> options,
    ILogger<CerrarReservasVencidasWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer no dispara al iniciar: las pruebas que no lo configuren no
        // observan mutaciones inesperadas en sus reservas sembradas.
        var intervalo = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervaloSegundos));
        using var timer = new PeriodicTimer(intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cerrar = scope.ServiceProvider.GetRequiredService<ICerrarReservasVencidas>();
                var completadas = await cerrar.EjecutarAsync(cancellationToken: stoppingToken);

                if (completadas > 0)
                {
                    logger.LogInformation(
                        "Cierre automático: {Completadas} reserva(s) completada(s).", completadas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error en el cierre automático de reservas.");
            }
        }
    }
}
