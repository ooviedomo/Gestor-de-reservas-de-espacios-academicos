using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record CompletarReservaCommand(Guid ReservaId) : ICommand;

/// <summary>
/// COM-001 (Command, ADR-002): coordinador y administrador completan manualmente una
/// reserva APROBADA. La máquina de estados valida la transición y registra la marca
/// de tiempo; el actor es opcional según la spec, pero aquí se registra para trazabilidad.
/// </summary>
public sealed class CompletarReserva(
    IReservaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual) : ICommandHandler<CompletarReservaCommand>
{
    public async Task<Result> HandleAsync(
        CompletarReservaCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result.Failure(DomainErrors.NoAutenticado);
        }

        var verificacion = guardia.VerificarCompletar();
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
            reserva.Completar(ActorTransicion.Usuario(usuarioId, usuarioActual.Rol));
            await repository.UpdateAsync(reserva, cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Code);
        }
    }
}
