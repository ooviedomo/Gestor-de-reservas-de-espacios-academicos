using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Espacios;

public interface IRegistrarSpace
{
    Task<Result<Guid>> HandleAsync(
        RegistrarSpaceCommand command,
        CancellationToken cancellationToken = default);
}
