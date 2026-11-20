using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record RegistrarSolicitanteCommand(
    string Nombre,
    string Email,
    string Contrasena) : ICommand;

/// <summary>USR-001: el autorregistro siempre crea un `Solicitante` activo.</summary>
public sealed class RegistrarSolicitante(IUsuarioIdentityService identity)
    : ICommandHandler<RegistrarSolicitanteCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(
        RegistrarSolicitanteCommand command,
        CancellationToken cancellationToken = default) =>
        identity.CrearAsync(
            new CrearUsuarioCommand(
                command.Nombre, command.Email, command.Contrasena, Rol.Solicitante),
            cancellationToken);
}
