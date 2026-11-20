using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>
/// Strategy (ADR-002): encapsula la regla de aforo que puede variar por tipo de
/// espacio. La implementación actual aplica la capacidad declarada a los tres tipos
/// del plan; sustituir la estrategia no obliga a cambiar la cadena de validación.
/// </summary>
public interface IValidacionPorTipoEspacio
{
    string? ValidarCapacidad(Space space, int asistentes);
}

public sealed class ValidacionPorTipoEspacio : IValidacionPorTipoEspacio
{
    public string? ValidarCapacidad(Space space, int asistentes) =>
        asistentes > space.Capacidad
            ? DomainErrors.CapacidadExcedida
            : null;
}
