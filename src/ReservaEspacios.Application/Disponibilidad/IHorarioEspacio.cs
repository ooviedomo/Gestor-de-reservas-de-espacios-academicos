using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Disponibilidad;

/// <summary>
/// DISP-001/003: regla de horario de atención en America/Bogota compartida por la
/// consulta de disponibilidad y por el validador de `CrearReserva` (tarea 3.2), de
/// modo que la conversión a UTC no esté duplicada (ADR-002: no duplicar reglas).
/// </summary>
public interface IHorarioEspacio
{
    bool Contiene(
        Space space, DateTimeOffset referencia, DateTimeOffset inicio, DateTimeOffset fin);
}

public sealed class HorarioEspacio : IHorarioEspacio
{
    private const string ZonaInstitucional = "America/Bogota";

    public bool Contiene(
        Space space, DateTimeOffset referencia, DateTimeOffset inicio, DateTimeOffset fin)
    {
        var (apertura, cierre) = EnUtc(space, referencia);
        return inicio >= apertura && fin <= cierre;
    }

    private static (DateTimeOffset Apertura, DateTimeOffset Cierre) EnUtc(
        Space space, DateTimeOffset referencia)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(ZonaInstitucional);
        var fechaLocal = TimeZoneInfo.ConvertTime(referencia, zona).Date;
        var aperturaLocal = DateTime.SpecifyKind(
            fechaLocal.Add(space.Horario.Apertura.ToTimeSpan()), DateTimeKind.Unspecified);
        var cierreLocal = DateTime.SpecifyKind(
            fechaLocal.Add(space.Horario.Cierre.ToTimeSpan()), DateTimeKind.Unspecified);

        return (
            new DateTimeOffset(aperturaLocal, zona.GetUtcOffset(aperturaLocal)).ToUniversalTime(),
            new DateTimeOffset(cierreLocal, zona.GetUtcOffset(cierreLocal)).ToUniversalTime());
    }
}
