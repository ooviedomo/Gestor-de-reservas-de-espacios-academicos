using System.Collections.Concurrent;
using ReservaEspacios.Application.Disponibilidad;

namespace ReservaEspacios.Infrastructure.Disponibilidad;

/// <summary>
/// Caché en memoria del decorator (ADR-002). <see cref="Invalidar"/> se invoca desde
/// el Decorator de escritura de reservas para que ninguna franja cacheada sobreviva
/// a una mutación de su espacio (tarea 3.1).
/// </summary>
public sealed class DisponibilidadCache : IDisponibilidadCache
{
    private readonly ConcurrentDictionary<DisponibilidadCacheKey, DisponibilidadDto> _entries = new();

    public DisponibilidadDto? Get(DisponibilidadCacheKey key) =>
        _entries.TryGetValue(key, out var value) ? value : null;

    public void Set(DisponibilidadCacheKey key, DisponibilidadDto value) =>
        _entries[key] = value;

    public void Invalidar(Guid spaceId)
    {
        foreach (var key in _entries.Keys.Where(k => k.SpaceId == spaceId))
        {
            _entries.TryRemove(key, out _);
        }
    }
}
