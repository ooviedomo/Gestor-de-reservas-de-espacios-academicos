using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reportes;

namespace ReservaEspacios.Application.Facade;

/// <summary>
/// Patrón Facade (ADR-002): única puerta de consulta para los componentes Razor,
/// que no necesitan conocer los casos de uso internos.
/// </summary>
public interface IReservaEspaciosFacade
{
    Task<Result<IReadOnlyList<SpaceCatalogoDto>>> ConsultarCatalogo(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default);

    Task<Result<DisponibilidadDto>> ConsultarDisponibilidad(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AuditoriaDto>>> ConsultarAuditoria(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken = default);

    Task<Result<ReporteOcupacionDto>> GenerarReporte(
        FiltroReporte filtro,
        CancellationToken cancellationToken = default);
}
