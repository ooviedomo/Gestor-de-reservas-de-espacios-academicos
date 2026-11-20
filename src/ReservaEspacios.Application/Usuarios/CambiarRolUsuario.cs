using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record CambiarRolUsuarioCommand(Guid UsuarioId, Rol Rol) : ICommand;

/// <summary>USR-002: cambiar roles es una operación exclusiva del administrador.</summary>
public sealed class CambiarRolUsuario(
    IUsuarioIdentityService identity,
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null) : ICommandHandler<CambiarRolUsuarioCommand>
{
    public async Task<Result> HandleAsync(
        CambiarRolUsuarioCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.Rol != Rol.Administrador)
        {
            return Result.Failure(DomainErrors.NoAutorizado);
        }

        var resultado = await identity.CambiarRolAsync(
            command.UsuarioId, command.Rol, cancellationToken);

        if (resultado.IsSuccess)
        {
            await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                new RolCambiado(
                    command.UsuarioId, command.Rol, usuarioActual.UsuarioId,
                    usuarioActual.Rol, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return resultado;
    }
}
