namespace ReservaEspacios.Application.Common;

/// <summary>
/// Implementación nula de <see cref="ITransaccionEventos"/> para handlers ejecutados sin
/// infraestructura de persistencia (pruebas unitarias). Ejecuta el delegado directamente.
/// </summary>
public sealed class TransaccionEventosNula : ITransaccionEventos
{
    public static readonly TransaccionEventosNula Instancia = new();

    private TransaccionEventosNula()
    {
    }

    public Task EjecutarAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancellationToken = default) =>
        operacion(cancellationToken);
}
