using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reportes;

/// <summary>
/// REP-001/002: aplica la misma Strategy de permisos que el resto de operaciones y
/// delega el cálculo en <see cref="ICalculoOcupacion"/>. No contabiliza PENDIENTE,
/// RECHAZADA ni CANCELADA: el repositorio filtra solo APROBADA y COMPLETADA.
/// </summary>
public sealed class ConsultarReporteOcupacion(
    IRepositorioOcupacion repositorio,
    ICalculoOcupacion calculo,
    GuardiaOperacionesProtegidas guardia)
{
    public async Task<Result<ReporteOcupacionDto>> HandleAsync(
        FiltroReporte filtro, CancellationToken cancellationToken = default)
    {
        var verificacion = guardia.VerificarReportes();
        if (!verificacion.IsSuccess)
        {
            return Result<ReporteOcupacionDto>.Failure(verificacion.ErrorCode!);
        }

        if (filtro.Hasta <= filtro.Desde)
        {
            return Result<ReporteOcupacionDto>.Failure(DomainErrors.RangoInvalido);
        }

        var datos = await repositorio.ObtenerPorPeriodoAsync(filtro, cancellationToken);
        var espacios = datos.Select(dato => calculo.Calcular(dato, filtro)).ToList();

        return Result<ReporteOcupacionDto>.Success(
            new ReporteOcupacionDto(espacios, filtro.Desde, filtro.Hasta));
    }
}
