using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// DISP-002/RES-002: decide si una franja se solapa con una reserva bloqueante,
/// usando intervalos semiabiertos. Lo consumen la consulta de disponibilidad y el
/// validador de solape de `CrearReserva` (tarea 3.2), sin duplicar la regla.
/// </summary>
public interface ISolapeReservas
{
    Task<bool> HaySolapeAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default);
}

public sealed class SolapeReservas(IDisponibilidadRepository repository) : ISolapeReservas
{
    public async Task<bool> HaySolapeAsync(
        Guid spaceId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        CancellationToken cancellationToken = default)
    {
        var inicioUtc = inicio.ToUniversalTime();
        var finUtc = fin.ToUniversalTime();
        var reservas = await repository.ObtenerReservasAsync(
            spaceId, inicioUtc, finUtc, cancellationToken);

        return reservas.Any(r =>
            EstadoReservaExtensions.DesdeCodigo(r.Estado)?.EsBloqueante() == true &&
            r.Inicio.ToUniversalTime() < finUtc &&
            inicioUtc < r.Fin.ToUniversalTime());
    }
}
