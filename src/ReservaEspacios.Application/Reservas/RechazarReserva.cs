using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record RechazarReservaCommand(Guid ReservaId, string Motivo) : ICommand;

/// <summary>
/// APR-001 (Command, ADR-002): el coordinador o el administrador rechazan una reserva
/// PENDIENTE exigiendo motivo. La regla de obligatoriedad y la transición viven en
/// la máquina de estados de Domain, no se duplican aquí.
/// </summary>
public sealed class RechazarReserva(
    IReservaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null,
    ITransaccionEventos? transaccion = null) : ICommandHandler<RechazarReservaCommand>
{
    public async Task<Result> HandleAsync(
        RechazarReservaCommand command,
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
            reserva.Rechazar(
                ActorTransicion.Usuario(usuarioId, usuarioActual.Rol), command.Motivo, ahora);
            var despachador = eventos ?? NullDomainEventDispatcher.Instancia;
            await (transaccion ?? TransaccionEventosNula.Instancia).EjecutarAsync(async token =>
            {
                await repository.UpdateAsync(reserva, token);
                await despachador.PublicarAsync(
                    new ReservaRechazada(
                        reserva.Id, reserva.SpaceId, reserva.UsuarioId, usuarioId, usuarioActual.Rol,
                        reserva.MotivoRechazo ?? command.Motivo, ahora),
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
