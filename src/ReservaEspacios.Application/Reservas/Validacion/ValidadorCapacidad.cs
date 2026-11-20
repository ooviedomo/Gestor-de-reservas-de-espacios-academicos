namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>RES-001: delega el aforo en el Strategy por tipo de espacio.</summary>
public sealed class ValidadorCapacidad(IValidacionPorTipoEspacio validacion)
    : ValidadorSincronoCrearReservaBase
{
    protected override string? Validar(ContextoValidacionReserva contexto) =>
        contexto.Space is null
            ? null
            : validacion.ValidarCapacidad(contexto.Space, contexto.Command.Asistentes);
}
