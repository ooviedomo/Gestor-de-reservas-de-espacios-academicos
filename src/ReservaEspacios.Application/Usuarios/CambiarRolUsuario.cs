using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record CambiarRolUsuarioCommand(Guid UsuarioId, Rol Rol) : ICommand;

/// <summary>USR-002: cambiar roles es una operación exclusiva del administrador.</summary>
public sealed class CambiarRolUsuario(
    IUsuarioIdentityService identity,
    IUsuarioActual usuarioActual) : ICommandHandler<CambiarRolUsuarioCommand>
{
    public Task<Result> HandleAsync(
        CambiarRolUsuarioCommand command,
        CancellationToken cancellationToken = default) =>
        usuarioActual.Rol == Rol.Administrador
            ? identity.CambiarRolAsync(command.UsuarioId, command.Rol, cancellationToken)
            : Task.FromResult(Result.Failure(DomainErrors.NoAutorizado));
}
