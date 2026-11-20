using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record AprobarReservaCommand(Guid ReservaId) : ICommand;

/// <summary>
/// APR-001 (Command, ADR-002): el coordinador o el administrador aprueban una reserva
/// PENDIENTE. El State concreto valida la transición y registra actor y fecha.
/// </summary>
public sealed class AprobarReserva(
    IReservaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null) : ICommandHandler<AprobarReservaCommand>
{
    public async Task<Result> HandleAsync(
        AprobarReservaCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result.Failure(DomainErrors.NoAutenticado);
        }

        var verificacion = guardia.VerificarAprobar();
        if (!verificacion.IsSuccess)
        {
            return Result.Failure(verificacion.ErrorCode!);
        }

        var reserva = await repository.GetByIdAsync(command.ReservaId, cancellationToken);
        if (reserva is null)
        {
            return Result.Failure(DomainErrors.ReservaNoEncontrada);
        }

        try
        {
            var ahora = DateTimeOffset.UtcNow;
            reserva.Aprobar(ActorTransicion.Usuario(usuarioId, usuarioActual.Rol), ahora);
            await repository.UpdateAsync(reserva, cancellationToken);
            await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                new ReservaAprobada(
                    reserva.Id, reserva.SpaceId, reserva.UsuarioId, usuarioId, usuarioActual.Rol,
                    reserva.AprobadoEn ?? ahora, ahora),
                cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Code);
        }
    }
}
