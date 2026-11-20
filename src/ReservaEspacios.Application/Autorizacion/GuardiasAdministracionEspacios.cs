using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Autorizacion;

/// <summary>
/// Decorator de autorización (AUTH-001/003): envuelve los casos de uso de espacios y
/// deniega por defecto toda acción no otorgada explícitamente al rol del usuario.
/// </summary>
public sealed class GuardiasAdministracionEspacios(
    RegistrarSpace registrar,
    EditarSpace editar,
    DesactivarSpace desactivar,
    ReactivarSpace reactivar,
    IEvaluadorPermisos permisos,
    IUsuarioActual usuarioActual)
    : IRegistrarSpace, IEditarSpace, IDesactivarSpace, IReactivarSpace
{
    public Task<Result<Guid>> HandleAsync(
        RegistrarSpaceCommand command,
        CancellationToken cancellationToken = default) =>
        Autorizado()
            ? registrar.HandleAsync(command, cancellationToken)
            : Task.FromResult(Result<Guid>.Failure(Error));

    public Task<Result> HandleAsync(
        EditarSpaceCommand command,
        CancellationToken cancellationToken = default) =>
        Autorizado()
            ? editar.HandleAsync(command, cancellationToken)
            : Task.FromResult(Result.Failure(Error));

    public Task<Result> HandleAsync(
        DesactivarSpaceCommand command,
        CancellationToken cancellationToken = default) =>
        Autorizado()
            ? desactivar.HandleAsync(command, cancellationToken)
            : Task.FromResult(Result.Failure(Error));

    public Task<Result> HandleAsync(
        ReactivarSpaceCommand command,
        CancellationToken cancellationToken = default) =>
        Autorizado()
            ? reactivar.HandleAsync(command, cancellationToken)
            : Task.FromResult(Result.Failure(Error));

    private bool Autorizado() =>
        usuarioActual.EstaAutenticado &&
        permisos.PuedeAdministrarEspacios(usuarioActual.Rol);

    private string Error => usuarioActual.EstaAutenticado
        ? DomainErrors.NoAutorizado
        : DomainErrors.NoAutenticado;
}
