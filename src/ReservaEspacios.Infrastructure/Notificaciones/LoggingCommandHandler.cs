using Microsoft.Extensions.Logging;
using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Infrastructure.Notificaciones;

/// <summary>
/// Decorator (ADR-002): envuelve el comando de correo para registrar su resultado sin
/// modificar al manejador ni al worker. Es la implementación efectiva registrada en DI.
/// </summary>
public sealed class LoggingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    ILogger<LoggingCommandHandler<TCommand>> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(
        TCommand command, CancellationToken cancellationToken = default)
    {
        var resultado = await inner.HandleAsync(command, cancellationToken);

        if (resultado.IsSuccess)
        {
            logger.LogInformation("Correo enviado correctamente ({Comando}).", typeof(TCommand).Name);
        }
        else
        {
            logger.LogWarning(
                "Fallo al enviar correo ({Comando}): {Error}.",
                typeof(TCommand).Name,
                resultado.ErrorCode);
        }

        return resultado;
    }
}
