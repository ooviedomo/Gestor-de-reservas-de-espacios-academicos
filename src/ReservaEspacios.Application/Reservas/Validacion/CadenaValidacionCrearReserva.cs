namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>
/// Compone el orden de la cadena de RES-001 según `design.md`:
/// Rango → EspacioActivo → Horario → Capacidad → Solape.
/// </summary>
public sealed class CadenaValidacionCrearReserva
{
    private readonly ValidadorCrearReservaBase _primero;

    public CadenaValidacionCrearReserva(
        ValidadorRango rango,
        ValidadorEspacioActivo espacioActivo,
        ValidadorHorario horario,
        ValidadorCapacidad capacidad,
        ValidadorSolape solape)
    {
        _primero = rango;
        rango.Encadenar(espacioActivo)
            .Encadenar(horario)
            .Encadenar(capacidad)
            .Encadenar(solape);
    }

    public ValueTask<string?> ValidarAsync(
        ContextoValidacionReserva contexto, CancellationToken cancellationToken = default) =>
        _primero.ValidarAsync(contexto, cancellationToken);
}
