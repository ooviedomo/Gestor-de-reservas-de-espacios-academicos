using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

public sealed record RegistrarSolicitanteCommand(
    string Nombre,
    string Email,
    string Contrasena) : ICommand;

/// <summary>USR-001: el autorregistro siempre crea un `Solicitante` activo.</summary>
public sealed class RegistrarSolicitante(
    IUsuarioIdentityService identity,
    IDomainEventDispatcher? eventos = null) : ICommandHandler<RegistrarSolicitanteCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(
        RegistrarSolicitanteCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultado = await identity.CrearAsync(
            new CrearUsuarioCommand(
                command.Nombre, command.Email, command.Contrasena, Rol.Solicitante),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            await (eventos ?? NullDomainEventDispatcher.Instancia).PublicarAsync(
                new UsuarioCreado(
                    resultado.Value, command.Nombre, command.Email, Rol.Solicitante,
                    ActorId: null, ActorRol: null, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return resultado;
    }
}
