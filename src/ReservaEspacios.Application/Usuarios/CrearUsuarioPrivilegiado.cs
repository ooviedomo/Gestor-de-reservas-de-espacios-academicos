using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record CrearUsuarioPrivilegiadoCommand(
    string Nombre,
    string Email,
    string Contrasena,
    Rol Rol) : ICommand;

/// <summary>USR-002: solo el administrador crea cuentas con privilegios.</summary>
public sealed class CrearUsuarioPrivilegiado(
    IUsuarioIdentityService identity,
    IUsuarioActual usuarioActual) : ICommandHandler<CrearUsuarioPrivilegiadoCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(
        CrearUsuarioPrivilegiadoCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.Rol != Rol.Administrador)
        {
            return Task.FromResult(Result<Guid>.Failure(DomainErrors.NoAutorizado));
        }

        if (command.Rol is not (Rol.Coordinador or Rol.Administrador))
        {
            return Task.FromResult(Result<Guid>.Failure(DomainErrors.NoAutorizado));
        }

        return identity.CrearAsync(
            new CrearUsuarioCommand(
                command.Nombre, command.Email, command.Contrasena, command.Rol),
            cancellationToken);
    }
}
