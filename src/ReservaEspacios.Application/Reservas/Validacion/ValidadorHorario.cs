using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>
/// RES-001: la franja debe estar dentro del horario de atención; reutiliza la misma
/// regla de zona horaria que la consulta de disponibilidad (DISP-003).
/// </summary>
public sealed class ValidadorHorario(IHorarioEspacio horario) : ValidadorSincronoCrearReservaBase
{
    protected override string? Validar(ContextoValidacionReserva contexto)
    {
        if (contexto.Space is null || contexto.Command.Fin <= contexto.Command.Inicio)
        {
            return null;
        }

        return horario.Contiene(
            contexto.Space,
            contexto.Command.Inicio,
            contexto.Command.Inicio.ToUniversalTime(),
            contexto.Command.Fin.ToUniversalTime())
            ? null
            : DomainErrors.FueraDeHorario;
    }
}
