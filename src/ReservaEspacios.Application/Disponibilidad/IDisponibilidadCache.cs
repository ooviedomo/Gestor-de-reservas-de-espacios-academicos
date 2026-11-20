namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// Clave normalizada a UTC para el decorator de caché (ADR-002).
/// La invalidación es por espacio: cualquier mutación de una reserva puede cambiar
/// el resultado de todas las franjas consultadas de ese espacio (tarea 3.1).
/// </summary>
public sealed record DisponibilidadCacheKey(
    Guid SpaceId,
    DateTimeOffset InicioUtc,
    DateTimeOffset FinUtc);

public interface IDisponibilidadCache
{
    DisponibilidadDto? Get(DisponibilidadCacheKey key);
    void Set(DisponibilidadCacheKey key, DisponibilidadDto value);

    /// <summary>Descarta todas las franjas cacheadas de un espacio tras una mutación.</summary>
    void Invalidar(Guid spaceId);
}
