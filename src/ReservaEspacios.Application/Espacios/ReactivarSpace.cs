using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Espacios;

public sealed record ReactivarSpaceCommand(Guid Id);

/// <summary>ESP-002: devuelve el espacio al catálogo y a las consultas de disponibilidad.</summary>
public sealed class ReactivarSpace(ISpaceRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(
        ReactivarSpaceCommand command,
        CancellationToken cancellationToken = default)
    {
        var space = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (space is null)
        {
            return Result.Failure(DomainErrors.EspacioNoEncontrado);
        }

        space.Reactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
