namespace ReservaEspacios.Domain.Common;

/// <summary>
/// Observer (ADR-002): contrato mínimo para un evento de dominio. Los suscriptores de
/// auditoría y outbox se registrarán en el tercer change; aquí solo se define el
/// contrato y el mecanismo de publicación.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OcurridoEn { get; }
}

/// <summary>Observer: suscriptor capaz de reaccionar a cualquier evento de dominio.</summary>
public interface IDomainEventSubscriber
{
    Task HandleAsync(IDomainEvent evento, CancellationToken cancellationToken = default);
}

/// <summary>Observer: publica un evento a todos los suscriptores registrados.</summary>
public interface IDomainEventDispatcher
{
    Task PublicarAsync(IDomainEvent evento, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación nula usada por defecto en los handlers y en pruebas unitarias que no
/// necesitan observar eventos. Evita obligar a cada caso de uso a conocer el despachador.
/// </summary>
public sealed class NullDomainEventDispatcher : IDomainEventDispatcher
{
    public static NullDomainEventDispatcher Instancia { get; } = new();

    private NullDomainEventDispatcher()
    {
    }

    public Task PublicarAsync(IDomainEvent evento, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
