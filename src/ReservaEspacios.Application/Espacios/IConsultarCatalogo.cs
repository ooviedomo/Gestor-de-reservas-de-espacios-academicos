using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Espacios;

public interface IConsultarCatalogo
{
    Task<Result<IReadOnlyList<SpaceCatalogoDto>>> HandleAsync(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default);
}
