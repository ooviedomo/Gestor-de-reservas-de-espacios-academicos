using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Espacios;

public interface ISpaceRepository
{
    Task<Space?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Space?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Space>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Space>> ListAsync(FiltroCatalogo filtro, CancellationToken cancellationToken = default);
    Task AddAsync(Space space, CancellationToken cancellationToken = default);
    void Remove(Space space);
}
