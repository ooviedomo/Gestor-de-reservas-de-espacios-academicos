using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// Builder (ADR-002): construye los filtros opcionales de `RES-004` sin obligar a la UI
/// ni al endpoint a conocer el orden de los parámetros del record `FiltroHistorial`.
/// </summary>
public sealed class FiltroHistorialBuilder
{
    private Guid? _usuarioId;
    private EstadoReserva? _estado;
    private DateTimeOffset? _desde;
    private DateTimeOffset? _hasta;
    private Guid? _spaceId;

    public FiltroHistorialBuilder ConUsuario(Guid? usuarioId)
    {
        _usuarioId = usuarioId;
        return this;
    }

    public FiltroHistorialBuilder ConEstado(EstadoReserva? estado)
    {
        _estado = estado;
        return this;
    }

    public FiltroHistorialBuilder Desde(DateTimeOffset? desde)
    {
        _desde = desde;
        return this;
    }

    public FiltroHistorialBuilder Hasta(DateTimeOffset? hasta)
    {
        _hasta = hasta;
        return this;
    }

    public FiltroHistorialBuilder ConSpace(Guid? spaceId)
    {
        _spaceId = spaceId;
        return this;
    }

    public FiltroHistorial Build() => new(_usuarioId, _estado, _desde, _hasta, _spaceId);
}
