using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Espacios;

public sealed class SpaceRepository(ReservaEspaciosDbContext context) : ISpaceRepository
{
    public Task<Space?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Spaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Space?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default) =>
        context.Spaces.FirstOrDefaultAsync(
            x => x.Nombre.ToLower() == nombre.Trim().ToLower(),
            cancellationToken);

    public async Task<IReadOnlyList<Space>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Spaces.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Space space, CancellationToken cancellationToken = default) =>
        await context.Spaces.AddAsync(space, cancellationToken);

    public void Remove(Space space) => context.Spaces.Remove(space);
}
