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

    public async Task<IReadOnlyList<Space>> ListAsync(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default)
    {
        var query = context.Spaces.AsNoTracking().AsQueryable();

        if (filtro.Tipo is { } tipo)
        {
            query = query.Where(x => x.Tipo == tipo);
        }

        if (filtro.CapacidadMinima is { } capacidadMinima)
        {
            query = query.Where(x => x.Capacidad >= capacidadMinima);
        }

        query = filtro.Estado switch
        {
            EstadoSpaceFiltro.Activo => query.Where(x => x.Activo),
            EstadoSpaceFiltro.Inactivo => query.Where(x => !x.Activo),
            _ => query
        };

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Space space, CancellationToken cancellationToken = default) =>
        await context.Spaces.AddAsync(space, cancellationToken);

    public void Remove(Space space) => context.Spaces.Remove(space);
}
