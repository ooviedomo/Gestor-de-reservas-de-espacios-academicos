namespace ReservaEspacios.Application.Common;

/// <summary>
/// Puerto de transacción (ADR-002, Adapter) que envuelve la mutación del caso de uso y
/// la publicación de sus eventos en una sola transacción. Así, el registro outbox y la
/// auditoría se confirman exactamente con el cambio de estado que los originó (NOT-001,
/// NOT-003, AUD-001). Sin implementación —casos de uso aislados— la operación se ejecuta
/// directamente, por lo que las pruebas unitarias existentes no se modifican.
/// </summary>
public interface ITransaccionEventos
{
    Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default);
}
