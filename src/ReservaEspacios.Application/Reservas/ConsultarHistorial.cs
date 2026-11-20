using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Reservas;

/// <summary>RES-004: filtros disponibles para el historial. La visibilidad por rol se aplica después.</summary>
public sealed record FiltroHistorial(
    Guid? UsuarioId = null,
    EstadoReserva? Estado = null,
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null,
    Guid? SpaceId = null);

/// <summary>RES-004: fila del historial con estados, fechas, motivos y actores legibles.</summary>
public sealed record HistorialReservaDto(
    Guid Id,
    Guid EspacioId,
    string Espacio,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    string Estado,
    string Solicitante,
    Guid? ActorId,
    string? Actor,
    string? RolActor,
    string? MotivoRechazo,
    string? MotivoCancelacion,
    DateTimeOffset? AprobadoEn,
    DateTimeOffset? CanceladoEn,
    DateTimeOffset? CompletadoEn,
    DateTimeOffset CreadoEn);

public sealed record ConsultarHistorialCommand(FiltroHistorial Filtro) : ICommand;

/// <summary>
/// RES-004 (Query/Facade, ADR-002): fuerza el alcance del solicitante a sus propias
/// reservas y deja ver todas a coordinador/administrador. Los nombres y roles se
/// resuelven mediante el Adapter de Identity; las reglas de visibilidad viven en la
/// Strategy de permisos, no en el endpoint.
/// </summary>
public sealed class ConsultarHistorial(
    IReservaRepository repository,
    ISpaceRepository spaces,
    IUsuarioIdentityService identity,
    IEvaluadorPermisos permisos,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual)
    : ICommandHandler<ConsultarHistorialCommand, IReadOnlyList<HistorialReservaDto>>
{
    public async Task<Result<IReadOnlyList<HistorialReservaDto>>> HandleAsync(
        ConsultarHistorialCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result<IReadOnlyList<HistorialReservaDto>>.Failure(DomainErrors.NoAutenticado);
        }

        var verificacion = guardia.VerificarHistorial();
        if (!verificacion.IsSuccess)
        {
            return Result<IReadOnlyList<HistorialReservaDto>>.Failure(verificacion.ErrorCode!);
        }

        // Un solicitante nunca puede ampliar su alcance con un filtro de usuario.
        var filtro = permisos.PuedeVerHistorialCompleto(usuarioActual.Rol)
            ? command.Filtro
            : command.Filtro with { UsuarioId = usuarioId };

        var reservas = await repository.ObtenerHistorialAsync(filtro, cancellationToken);
        var espacios = (await spaces.ListAsync(cancellationToken))
            .ToDictionary(space => space.Id, space => space.Nombre);

        var identificadores = reservas.Select(r => r.UsuarioId)
            .Concat(reservas.Where(r => r.ActorId.HasValue).Select(r => r.ActorId!.Value))
            .Distinct();
        var usuarios = new Dictionary<Guid, DatosActorDto>();
        foreach (var id in identificadores)
        {
            var dato = await identity.ObtenerPorIdAsync(id, cancellationToken);
            if (dato.Value is { } valor)
            {
                usuarios[id] = valor;
            }
        }

        var historial = reservas.Select(reserva => new HistorialReservaDto(
            reserva.Id,
            reserva.SpaceId,
            espacios.GetValueOrDefault(reserva.SpaceId, "Espacio desconocido"),
            reserva.Inicio,
            reserva.Fin,
            reserva.Estado.Codigo(),
            usuarios.GetValueOrDefault(reserva.UsuarioId)?.Nombre ?? "Solicitante desconocido",
            reserva.ActorId,
            reserva.ActorId is { } actorId
                ? usuarios.GetValueOrDefault(actorId)?.Nombre
                : null,
            reserva.ActorId is { } rolActorId &&
            usuarios.TryGetValue(rolActorId, out var actor)
                ? actor.Rol.Etiqueta()
                : null,
            reserva.MotivoRechazo,
            reserva.MotivoCancelacion,
            reserva.AprobadoEn,
            reserva.CanceladoEn,
            reserva.CompletadoEn,
            reserva.CreadoEn)).ToArray();

        return Result<IReadOnlyList<HistorialReservaDto>>.Success(historial);
    }
}
