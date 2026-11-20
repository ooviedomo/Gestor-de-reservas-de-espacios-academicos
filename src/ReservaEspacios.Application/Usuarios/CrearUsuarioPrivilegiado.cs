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
    IUsuarioActual usuarioActual,
    IDomainEventDispatcher? eventos = null) : ICommandHandler<CrearUsuarioPrivilegiadoCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        CrearUsuarioPrivilegiadoCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.Rol != Rol.Administrador)
        {
            return Result<Guid>.Failure(DomainErrors.NoAutorizado);
        }

        if (command.Rol is not (Rol.Coordinador or Rol.Administrador))
        {
            return Result<Guid>.Failure(DomainErrors.NoAutorizado);
        }

        var resultado = await identity.CrearAsync(
            new CrearUsuarioCommand(
                command.Nombre, command.Email, command.Contrasena, command.Rol),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                new UsuarioCreado(
                    resultado.Value, command.Nombre, command.Email, command.Rol,
                    usuarioActual.UsuarioId, usuarioActual.Rol, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return resultado;
    }
}
