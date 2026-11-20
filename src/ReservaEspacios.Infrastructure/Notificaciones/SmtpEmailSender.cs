using System.Net.Mail;
using Microsoft.Extensions.Options;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Notificaciones;

namespace ReservaEspacios.Infrastructure.Notificaciones;

/// <summary>
/// Adapter (ADR-002): implementa <see cref="IEmailSender"/> sobre SMTP estándar de .NET,
/// sin nuevas dependencias NuGet. Un host vacío se traduce en fallo controlado para que
/// el worker registre el motivo en `ultimo_error` sin detener la aplicación.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task<Result> EnviarAsync(
        EnviarCorreoCommand command, CancellationToken cancellationToken = default)
    {
        var configuracion = options.Value;
        if (string.IsNullOrWhiteSpace(configuracion.Host))
        {
            return Result.Failure("SMTP_NO_CONFIGURADO");
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(configuracion.Remitente),
                Subject = command.Asunto,
                Body = command.Cuerpo,
                IsBodyHtml = false
            };
            mensaje.To.Add(command.Destinatario);

            using var cliente = new SmtpClient(configuracion.Host, configuracion.Puerto)
            {
                EnableSsl = configuracion.UsarSsl,
                Credentials = string.IsNullOrWhiteSpace(configuracion.Usuario)
                    ? null
                    : new System.Net.NetworkCredential(
                        configuracion.Usuario, configuracion.Contrasena)
            };

            await cliente.SendMailAsync(mensaje, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (
            exception is SmtpException or FormatException or InvalidOperationException)
        {
            return Result.Failure($"SMTP_FALLO: {exception.Message}");
        }
    }
}
