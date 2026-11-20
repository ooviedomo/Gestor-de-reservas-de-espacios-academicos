using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Auditoria;

/// <summary>
/// AUD-001: comando de prueba de la restricción de inmutabilidad. El repositorio no
/// expone borrado real; este comando solo comprueba que la base de datos rechaza el
/// intento y traduce el error a un código estable. Está restringido al administrador.
/// </summary>
public sealed record EliminarAuditoriaCommand(Guid RegistroId) : ICommand;

public sealed class EliminarAuditoria(
    IAuditoriaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual) : ICommandHandler<EliminarAuditoriaCommand>
{
    public async Task<Result> HandleAsync(
        EliminarAuditoriaCommand command,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActual.Rol != Rol.Administrador)
        {
            return Result.Failure(DomainErrors.NoAutorizado);
        }

        var verificacion = guardia.VerificarAuditoria();
        return !verificacion.IsSuccess
            ? Result.Failure(verificacion.ErrorCode!)
            : await repository.IntentarEliminarAsync(command.RegistroId, cancellationToken);
    }
}
