using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Espacios;

public interface IReactivarSpace
{
    Task<Result> HandleAsync(
        ReactivarSpaceCommand command,
        CancellationToken cancellationToken = default);
}
