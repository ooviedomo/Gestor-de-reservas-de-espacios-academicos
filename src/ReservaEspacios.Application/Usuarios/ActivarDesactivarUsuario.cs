using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record ActivarDesactivarUsuarioCommand(Guid UsuarioId, bool Activo) : ICommand;

/// <summary>USR-002:activar/desactivar cuentas es una operación exclusiva del administrador.</summary>
public sealed class ActivarDesactivarUsuario(
    IUsuarioIdentityService identity,
    IUsuarioActual usuarioActual) : ICommandHandler<ActivarDesactivarUsuarioCommand>
{
    public Task<Result> HandleAsync(
        ActivarDesactivarUsuarioCommand command,
        CancellationToken cancellationToken = default) =>
        usuarioActual.Rol == Rol.Administrador
            ? identity.ActivarDesactivarAsync(command.UsuarioId, command.Activo, cancellationToken)
            : Task.FromResult(Result.Failure(DomainErrors.NoAutorizado));
}
