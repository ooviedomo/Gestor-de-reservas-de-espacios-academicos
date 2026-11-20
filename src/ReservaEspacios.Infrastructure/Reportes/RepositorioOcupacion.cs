using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Reportes;

/// <summary>
/// Adapter que deriva la ocupación de las reservas existentes, sin contadores duplicados.
/// El número de reservas se obtiene con `GROUP BY` en PostgreSQL; los intervalos se leen
/// aparte para que <see cref="ICalculoOcupacion"/> pueda unirlos sin duplicar solapes.
/// </summary>
public sealed class RepositorioOcupacion(ReservaEspaciosDbContext context)
    : IRepositorioOcupacion
{
    public async Task<IReadOnlyList<DatosOcupacionEspacio>> ObtenerPorPeriodoAsync(
        FiltroReporte filtro, CancellationToken cancellationToken = default)
    {
        var espacios = await context.Spaces
            .Where(s => filtro.SpaceId == null || s.Id == filtro.SpaceId)
            .ToListAsync(cancellationToken);

        var residentes = context.Reservations.Where(r =>
            (r.Estado == EstadoReserva.Aprobada || r.Estado == EstadoReserva.Completada) &&
            r.Inicio < filtro.Hasta && r.Fin > filtro.Desde);

        if (filtro.SpaceId is { } espacioId)
        {
            residentes = residentes.Where(r => r.SpaceId == espacioId);
        }

        var conteos = await residentes
            .GroupBy(r => r.SpaceId)
            .Select(grupo => new { SpaceId = grupo.Key, Reservas = grupo.Count() })
            .ToListAsync(cancellationToken);

        var intervalos = await residentes
            .Select(r => new { r.SpaceId, r.Inicio, r.Fin })
            .ToListAsync(cancellationToken);

        var conteoPorEspacio = conteos.ToDictionary(x => x.SpaceId, x => x.Reservas);

        return espacios.Select(espacio => new DatosOcupacionEspacio(
            espacio.Id,
            espacio.Nombre,
            espacio.Horario.Apertura,
            espacio.Horario.Cierre,
            conteoPorEspacio.GetValueOrDefault(espacio.Id),
            intervalos
                .Where(x => x.SpaceId == espacio.Id)
                .Select(x => new IntervaloReserva(x.Inicio, x.Fin))
                .ToList())).ToList();
    }
}
