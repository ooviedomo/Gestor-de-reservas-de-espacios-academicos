using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Usuarios;

public sealed record AutenticarUsuarioCommand(string Email, string Contrasena) : ICommand;

/// <summary>USR-003: delega en Identity el manejo de credenciales y la sesión por rol.</summary>
public sealed class AutenticarUsuario(IUsuarioIdentityService identity)
    : ICommandHandler<AutenticarUsuarioCommand, UsuarioSesionDto>
{
    public Task<Result<UsuarioSesionDto>> HandleAsync(
        AutenticarUsuarioCommand command,
        CancellationToken cancellationToken = default) =>
        identity.AutenticarAsync(command.Email, command.Contrasena, cancellationToken);
}
