namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>
/// Chain of Responsibility (ADR-002): cada eslabón valida y delega en el siguiente.
/// Devuelve el primer código de error o <c>null</c> si toda la cadena pasó.
/// </summary>
public abstract class ValidadorCrearReservaBase
{
    private ValidadorCrearReservaBase? _siguiente;

    public ValidadorCrearReservaBase Encadenar(ValidadorCrearReservaBase siguiente)
    {
        _siguiente = siguiente;
        return siguiente;
    }

    public async ValueTask<string?> ValidarAsync(
        ContextoValidacionReserva contexto,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidarInternoAsync(contexto, cancellationToken);
        return error ?? await (_siguiente?.ValidarAsync(contexto, cancellationToken)
            ?? new ValueTask<string?>((string?)null));
    }

    protected abstract ValueTask<string?> ValidarInternoAsync(
        ContextoValidacionReserva contexto, CancellationToken cancellationToken);
}

/// <summary>Eslabones sin E/S; evita repetir <c>Task.FromResult</c> en cada validador.</summary>
public abstract class ValidadorSincronoCrearReservaBase : ValidadorCrearReservaBase
{
    protected abstract string? Validar(ContextoValidacionReserva contexto);

    protected override ValueTask<string?> ValidarInternoAsync(
        ContextoValidacionReserva contexto, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Validar(contexto));
}
