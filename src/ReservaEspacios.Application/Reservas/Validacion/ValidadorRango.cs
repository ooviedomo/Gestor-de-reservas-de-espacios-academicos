using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>RES-001: el inicio debe ser anterior al fin.</summary>
public sealed class ValidadorRango : ValidadorSincronoCrearReservaBase
{
    protected override string? Validar(ContextoValidacionReserva contexto) =>
        contexto.Command.Fin <= contexto.Command.Inicio
            ? DomainErrors.RangoInvalido
            : null;
}
