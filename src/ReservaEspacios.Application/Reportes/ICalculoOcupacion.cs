namespace ReservaEspacios.Application.Reportes;

/// <summary>
/// Strategy (ADR-002): concentra las reglas de REP-003 —denominador en días naturales de
/// `America/Bogota` y unión de intervalos para no duplicar franjas solapadas.
/// </summary>
public interface ICalculoOcupacion
{
    DetalleOcupacionDto Calcular(DatosOcupacionEspacio datos, FiltroReporte filtro);
}

public sealed class CalculoOcupacion : ICalculoOcupacion
{
    private const string ZonaInstitucional = "America/Bogota";

    public DetalleOcupacionDto Calcular(DatosOcupacionEspacio datos, FiltroReporte filtro)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(ZonaInstitucional);
        var desdeLocal = TimeZoneInfo.ConvertTime(filtro.Desde, zona);
        var hastaLocal = TimeZoneInfo.ConvertTime(filtro.Hasta, zona);
        var diasNaturales = Math.Max(1, (hastaLocal.Date - desdeLocal.Date).Days + 1);

        var horasDisponibles = (datos.Cierre - datos.Apertura).TotalHours * diasNaturales;
        var horasReservadas = SumarUnion(datos.Intervalos);
        var porcentaje = horasDisponibles <= 0
            ? 0
            : Math.Min(100, Math.Round(horasReservadas / horasDisponibles * 100, 2));

        return new DetalleOcupacionDto(
            datos.SpaceId, datos.Nombre, datos.NumeroReservas,
            Math.Round(horasReservadas, 2), Math.Round(horasDisponibles, 2), porcentaje);
    }

    private static double SumarUnion(IReadOnlyList<IntervaloReserva> intervalos)
    {
        var ordenados = intervalos
            .Select(x => (Inicio: x.Inicio.ToUniversalTime(), Fin: x.Fin.ToUniversalTime()))
            .Where(x => x.Fin > x.Inicio)
            .OrderBy(x => x.Inicio)
            .ToList();

        if (ordenados.Count == 0)
        {
            return 0;
        }

        var total = 0d;
        var (inicioUnion, finUnion) = ordenados[0];

        foreach (var actual in ordenados.Skip(1))
        {
            if (actual.Inicio <= finUnion)
            {
                if (actual.Fin > finUnion)
                {
                    finUnion = actual.Fin;
                }
            }
            else
            {
                total += (finUnion - inicioUnion).TotalHours;
                (inicioUnion, finUnion) = actual;
            }
        }

        return total + (finUnion - inicioUnion).TotalHours;
    }
}
