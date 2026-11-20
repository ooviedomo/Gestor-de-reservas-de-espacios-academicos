using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Common;

/// <summary>
/// Observer (ADR-002): despacha cada evento a todos los suscriptores registrados en DI.
/// No usa MediatR: la dependencia no está en la stack del plan §3.2. Si no hay
/// suscriptores (este change), la publicación es una operación vacía y segura.
/// </summary>
public sealed class DomainEventDispatcher(
    IEnumerable<IDomainEventSubscriber> suscriptores) : IDomainEventDispatcher
{
    public async Task PublicarAsync(
        IDomainEvent evento, CancellationToken cancellationToken = default)
    {
        foreach (var suscriptor in suscriptores)
        {
            await suscriptor.HandleAsync(evento, cancellationToken);
        }
    }
}
