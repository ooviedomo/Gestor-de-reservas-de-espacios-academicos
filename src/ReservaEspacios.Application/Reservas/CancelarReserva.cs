using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record CancelarReservaCommand(Guid ReservaId, string Motivo) : ICommand;

/// <summary>
/// RES-003 (Command, ADR-002): exige autenticación, aplica la matriz de permisos sobre
/// la propiedad de la reserva y delega la transición y el motivo obligatorio en la
/// máquina de estados de Domain. El Estado concreto es la fuente de verdad de qué
/// estados pueden cancelarse y de quién puede hacerlo (State + Template Method).
/// </summary>
public sealed class CancelarReserva(
    IReservaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null,
    ITransaccionEventos? transaccion = null) : ICommandHandler<CancelarReservaCommand>
{
    public async Task<Result> HandleAsync(
        CancelarReservaCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result.Failure(DomainErrors.NoAutenticado);
        }

        var reserva = await repository.GetByIdAsync(command.ReservaId, cancellationToken);
        if (reserva is null)
        {
            return Result.Failure(DomainErrors.ReservaNoEncontrada);
        }

        var verificacion = guardia.VerificarCancelar(reserva.UsuarioId, usuarioId);
        if (!verificacion.IsSuccess)
        {
            return Result.Failure(verificacion.ErrorCode!);
        }

        try
        {
            var ahora = DateTimeOffset.UtcNow;
            reserva.Cancelar(
                ActorTransicion.Usuario(usuarioId, usuarioActual.Rol), command.Motivo, ahora);
            var despachador = eventos ?? NullDomainEventDispatcher.Instancia;
            await (transaccion ?? TransaccionEventosNula.Instancia).EjecutarAsync(async token =>
            {
                await repository.UpdateAsync(reserva, token);
                await despachador.PublicarAsync(
                    new ReservaCancelada(
                        reserva.Id, reserva.SpaceId, reserva.UsuarioId, usuarioId, usuarioActual.Rol,
                        reserva.MotivoCancelacion ?? command.Motivo, ahora),
                    token);
            }, cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Code);
        }
    }
}
