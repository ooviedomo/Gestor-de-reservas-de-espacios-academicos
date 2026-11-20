using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// COM-001: cierre automático de reservas APROBADA cuyo <c>fin</c> venció. Se expone
/// como abstracción para que el worker (Web) y las pruebas no dependan del manejador
/// concreto (ADR-002, Command).
/// </summary>
public interface ICerrarReservasVencidas
{
    /// <summary>Devuelve cuántas reservas se completaron en esta ejecución.</summary>
    Task<int> EjecutarAsync(
        DateTimeOffset? ahora = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Consulta las reservas vencidas y delega la transición en la máquina de estados con
/// <see cref="ActorTransicion.Sistema"/>. No consulta <c>IUsuarioActual</c>: el cierre
/// automático no tiene usuario. Es idempotente: si otra ejecución ya completó la reserva,
/// la transición falla y se ignora sin afectar al resto del lote.
/// </summary>
public sealed class CerrarReservasVencidas(
    IReservaRepository repository,
    IDomainEventDispatcher? eventos = null) : ICerrarReservasVencidas
{
    public async Task<int> EjecutarAsync(
        DateTimeOffset? ahora = null, CancellationToken cancellationToken = default)
    {
        var momento = (ahora ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var vencidas = await repository.ObtenerAprobadasVencidasAsync(momento, cancellationToken);
        var completadas = 0;

        foreach (var reserva in vencidas)
        {
            try
            {
                reserva.CompletarAutomaticamente(momento);
                await repository.UpdateAsync(reserva, cancellationToken);
                await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                    new ReservaCompletada(
                        reserva.Id, reserva.SpaceId, reserva.UsuarioId, null, null,
                        reserva.CompletadoEn ?? momento, Automatico: true, momento),
                    cancellationToken);
                completadas++;
            }
            catch (DomainException)
            {
                // Otra ejecución ganó la carrera; el estado objetivo ya está alcanzado.
            }
        }

        return completadas;
    }
}
