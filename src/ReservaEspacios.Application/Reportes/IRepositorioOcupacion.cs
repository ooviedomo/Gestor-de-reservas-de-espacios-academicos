namespace ReservaEspacios.Application.Reportes;

/// <summary>Puerto de consulta agregada; la implementación usa `GROUP BY` sobre reservas.</summary>
public interface IRepositorioOcupacion
{
    Task<IReadOnlyList<DatosOcupacionEspacio>> ObtenerPorPeriodoAsync(
        FiltroReporte filtro, CancellationToken cancellationToken = default);
}
