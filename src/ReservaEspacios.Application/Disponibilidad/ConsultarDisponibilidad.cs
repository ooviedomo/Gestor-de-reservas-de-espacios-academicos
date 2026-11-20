using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Disponibilidad;

public sealed record ConsultarDisponibilidadCommand(
    Guid SpaceId,
    DateTimeOffset Inicio,
    DateTimeOffset Fin);

public interface IConsultarDisponibilidad
{
    Task<Result<DisponibilidadDto>> HandleAsync(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DISP-001 a DISP-003: horario de atención en America/Bogota convertido a UTC,
/// ocupación solo de reservas PENDIENTE/APROBADA e intervalos semiabiertos.
/// Las reglas de horario y solape se delegan en servicios compartidos con la cadena
/// de validación de `CrearReserva` (tarea 3.2). Los parámetros opcionales preservan
/// las pruebas unitarias existentes, que solo mockean los dos puertos originales.
/// </summary>
public sealed class ConsultarDisponibilidad(
    ISpaceRepository spaceRepository,
    IDisponibilidadRepository disponibilidadRepository,
    IHorarioEspacio? horario = null,
    ISolapeReservas? solape = null) : IConsultarDisponibilidad
{
    private readonly IHorarioEspacio _horario = horario ?? new HorarioEspacio();
    private readonly ISolapeReservas _solape = solape ?? new SolapeReservas(disponibilidadRepository);

    public async Task<Result<DisponibilidadDto>> HandleAsync(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default)
    {
        var space = await spaceRepository.GetByIdAsync(command.SpaceId, cancellationToken);
        if (space is null)
        {
            return Result<DisponibilidadDto>.Failure(DomainErrors.EspacioNoEncontrado);
        }

        if (!space.Activo)
        {
            return Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.EspacioInactivo);
        }

        var inicio = command.Inicio.ToUniversalTime();
        var fin = command.Fin.ToUniversalTime();
        if (fin <= inicio)
        {
            return Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.FueraDeHorario);
        }

        if (!_horario.Contiene(space, command.Inicio, inicio, fin))
        {
            return Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.FueraDeHorario);
        }

        var haySolape = await _solape.HaySolapeAsync(space.Id, inicio, fin, cancellationToken);

        return haySolape
            ? Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.SolapeConReserva)
            : Resultado(EstadosDisponibilidad.Disponible, MotivosDisponibilidad.Ninguno);
    }

    private static Result<DisponibilidadDto> Resultado(string estado, string motivo) =>
        Result<DisponibilidadDto>.Success(new DisponibilidadDto(estado, motivo));
}
