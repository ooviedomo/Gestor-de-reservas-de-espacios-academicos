using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Espacios;

public interface IDesactivarSpace
{
    Task<Result> HandleAsync(
        DesactivarSpaceCommand command,
        CancellationToken cancellationToken = default);
}
