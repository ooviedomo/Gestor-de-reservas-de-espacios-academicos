using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Notificaciones;

/// <summary>
/// Command (ADR-002): instrucción diferida que el worker extrae del outbox. Se guarda
/// como fila en `notifications` y se ejecuta fuera de la transacción de la reserva.
/// </summary>
public sealed record EnviarCorreoCommand(string Destinatario, string Asunto, string Cuerpo) : ICommand;

/// <summary>Adapter (ADR-002): proveedor externo de correo, sustituible en pruebas.</summary>
public interface IEmailSender
{
    Task<Result> EnviarAsync(
        EnviarCorreoCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Command handler que desacopla el procesador del proveedor concreto.</summary>
public sealed class EnviarCorreoHandler(IEmailSender sender) : ICommandHandler<EnviarCorreoCommand>
{
    public Task<Result> HandleAsync(
        EnviarCorreoCommand command, CancellationToken cancellationToken = default) =>
        sender.EnviarAsync(command, cancellationToken);
}
