using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas.Validacion;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record CrearReservaCommand(
    Guid SpaceId,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    string Proposito,
    int Asistentes) : ICommand;

/// <summary>
/// RES-001/002 (Command, ADR-002): valida con la cadena
/// Rango → EspacioActivo → Horario → Capacidad → Solape, y luego revalida la
/// disponibilidad dentro de una transacción SERIALIZABLE antes de persistir.
/// La consulta previa es informativa; la confirmación vuelve a comprobar.
/// </summary>
public sealed class CrearReserva(
    IReservaRepository repository,
    ISpaceRepository spaceRepository,
    ITransaccionReserva transaccion,
    CadenaValidacionCrearReserva validadores,
    ValidadorSolape revalidador,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null) : ICommandHandler<CrearReservaCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CrearReservaCommand command,
        CancellationToken cancellationToken = default)
    {
        var verificacion = guardia.VerificarCrearReserva();
        if (!verificacion.IsSuccess)
        {
            return Result<Guid>.Failure(verificacion.ErrorCode!);
        }

        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result<Guid>.Failure(DomainErrors.NoAutenticado);
        }

        try
        {
            var space = await spaceRepository.GetByIdAsync(command.SpaceId, cancellationToken);
            var contexto = new ContextoValidacionReserva(command, usuarioId, space);

            var error = await validadores.ValidarAsync(contexto, cancellationToken);
            if (error is not null)
            {
                return Result<Guid>.Failure(error);
            }

            Reservation? reserva = null;
            await transaccion.EjecutarAsync(async token =>
            {
                // Revalidación dentro de SERIALIZABLE: otra reserva pudo ocupar la
                // franja entre la consulta informativa y esta confirmación (RES-001).
                var errorRevalidacion = await revalidador.ValidarAsync(contexto, token);
                if (errorRevalidacion is not null)
                {
                    throw new DomainException(
                        errorRevalidacion,
                        "La franja dejó de estar disponible antes de confirmar.");
                }

                reserva = new Reservation(
                    Guid.NewGuid(),
                    command.SpaceId,
                    usuarioId,
                    command.Inicio,
                    command.Fin,
                    command.Proposito,
                    command.Asistentes);

                await repository.AddAsync(reserva, token);
                // AUD-001: la auditoría del alta comparte la transacción SERIALIZABLE.
                // NOT-001: ReservaCreada no genera correo; el outbox solo reacciona a
                // aprobación, rechazo y cancelación.
                await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                    new ReservaCreada(
                        reserva.Id, reserva.SpaceId, reserva.UsuarioId, usuarioId,
                        usuarioActual.Rol, reserva.Inicio, reserva.Fin, reserva.CreadoEn),
                    token);
            }, cancellationToken);

            return Result<Guid>.Success(reserva!.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Code);
        }
    }
}
