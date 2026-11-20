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
/// </summary>
public sealed class ConsultarDisponibilidad(
    ISpaceRepository spaceRepository,
    IDisponibilidadRepository disponibilidadRepository) : IConsultarDisponibilidad
{
    private const string ZonaInstitucional = "America/Bogota";

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

        var (apertura, cierre) = HorarioEnUtc(space, command.Inicio);
        if (inicio < apertura || fin > cierre)
        {
            return Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.FueraDeHorario);
        }

        var reservas = await disponibilidadRepository.ObtenerReservasAsync(
            space.Id, inicio, fin, cancellationToken);

        var solape = reservas.Any(reserva =>
            EsBloqueante(reserva.Estado) &&
            reserva.Inicio.ToUniversalTime() < fin &&
            inicio < reserva.Fin.ToUniversalTime());

        return solape
            ? Resultado(EstadosDisponibilidad.NoDisponible, MotivosDisponibilidad.SolapeConReserva)
            : Resultado(EstadosDisponibilidad.Disponible, MotivosDisponibilidad.Ninguno);
    }

    private static bool EsBloqueante(string estado) =>
        estado is "PENDIENTE" or "APROBADA";

    private static (DateTimeOffset Apertura, DateTimeOffset Cierre) HorarioEnUtc(
        Domain.Espacios.Space space,
        DateTimeOffset referencia)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(ZonaInstitucional);
        var fechaLocal = TimeZoneInfo.ConvertTime(referencia, zona).Date;
        var aperturaLocal = DateTime.SpecifyKind(
            fechaLocal.Add(space.Horario.Apertura.ToTimeSpan()), DateTimeKind.Unspecified);
        var cierreLocal = DateTime.SpecifyKind(
            fechaLocal.Add(space.Horario.Cierre.ToTimeSpan()), DateTimeKind.Unspecified);

        return (
            new DateTimeOffset(aperturaLocal, zona.GetUtcOffset(aperturaLocal)).ToUniversalTime(),
            new DateTimeOffset(cierreLocal, zona.GetUtcOffset(cierreLocal)).ToUniversalTime());
    }

    private static Result<DisponibilidadDto> Resultado(string estado, string motivo) =>
        Result<DisponibilidadDto>.Success(new DisponibilidadDto(estado, motivo));
}
