using System.Collections.Concurrent;
using ReservaEspacios.Application.Disponibilidad;

namespace ReservaEspacios.Infrastructure.Disponibilidad;

/// <summary>
/// Caché en memoria del decorator. Se invalida cuando el siguiente change incorpora
/// la tabla `reservations` y pasan a existir mutaciones que afecten la franja.
/// </summary>
public sealed class DisponibilidadCache : IDisponibilidadCache
{
    private readonly ConcurrentDictionary<DisponibilidadCacheKey, DisponibilidadDto> _entries = new();

    public DisponibilidadDto? Get(DisponibilidadCacheKey key) =>
        _entries.TryGetValue(key, out var value) ? value : null;

    public void Set(DisponibilidadCacheKey key, DisponibilidadDto value) =>
        _entries[key] = value;
}
