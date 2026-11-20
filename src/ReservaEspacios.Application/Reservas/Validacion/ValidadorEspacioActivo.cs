using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>RES-001: el espacio debe existir y estar activo.</summary>
public sealed class ValidadorEspacioActivo : ValidadorSincronoCrearReservaBase
{
    protected override string? Validar(ContextoValidacionReserva contexto) =>
        contexto.Space is null
            ? DomainErrors.EspacioNoEncontrado
            : !contexto.Space.Activo
                ? DomainErrors.EspacioInactivo
                : null;
}
