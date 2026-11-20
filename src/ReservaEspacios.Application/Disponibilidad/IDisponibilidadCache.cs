namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>Clave normalizada a UTC para el decorator de caché (ADR-002).</summary>
public sealed record DisponibilidadCacheKey(
    Guid SpaceId,
    DateTimeOffset InicioUtc,
    DateTimeOffset FinUtc);

public interface IDisponibilidadCache
{
    DisponibilidadDto? Get(DisponibilidadCacheKey key);
    void Set(DisponibilidadCacheKey key, DisponibilidadDto value);
}
