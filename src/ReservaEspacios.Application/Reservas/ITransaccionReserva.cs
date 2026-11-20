namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// Puerto de transacción (ADR-002): permite ejecutar la revalidación y la escritura
/// de una reserva en una única transacción SERIALIZABLE sin acoplar Application a
/// EF Core ni a Npgsql. Si la operación lanza, la implementación hace rollback.
/// </summary>
public interface ITransaccionReserva
{
    Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default);
}
