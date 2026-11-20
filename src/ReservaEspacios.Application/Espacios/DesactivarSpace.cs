using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Espacios;

public sealed record DesactivarSpaceCommand(Guid Id);

/// <summary>
/// ESP-002: oculta el espacio de nuevas consultas sin eliminar sus reservas existentes.
/// </summary>
public sealed class DesactivarSpace(ISpaceRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(
        DesactivarSpaceCommand command,
        CancellationToken cancellationToken = default)
    {
        var space = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (space is null)
        {
            return Result.Failure(DomainErrors.EspacioNoEncontrado);
        }

        space.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
