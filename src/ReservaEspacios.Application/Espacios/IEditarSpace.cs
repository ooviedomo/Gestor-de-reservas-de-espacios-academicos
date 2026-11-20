using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Espacios;

public interface IEditarSpace
{
    Task<Result> HandleAsync(
        EditarSpaceCommand command,
        CancellationToken cancellationToken = default);
}
