using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>
/// RES-001/002: último eslabón de la cadena. Reutiliza `ISolapeReservas`, la misma
/// regla que usa la consulta de disponibilidad, y la reejecuta dentro de la
/// transacción SERIALIZABLE antes de confirmar (tarea 3.2).
/// </summary>
public sealed class ValidadorSolape(ISolapeReservas solape) : ValidadorCrearReservaBase
{
    protected override async ValueTask<string?> ValidarInternoAsync(
        ContextoValidacionReserva contexto, CancellationToken cancellationToken)
    {
        if (contexto.Space is null || contexto.Command.Fin <= contexto.Command.Inicio)
        {
            return null;
        }

        var haySolape = await solape.HaySolapeAsync(
            contexto.Command.SpaceId,
            contexto.Command.Inicio.ToUniversalTime(),
            contexto.Command.Fin.ToUniversalTime(),
            cancellationToken);

        return haySolape ? DomainErrors.SolapeDetectado : null;
    }
}
