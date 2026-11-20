using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Facade;

public sealed class ReservaEspaciosFacade(
    IConsultarCatalogo consultarCatalogo,
    IConsultarDisponibilidad consultarDisponibilidad,
    ConsultarAuditoria? consultarAuditoria = null,
    ConsultarReporteOcupacion? consultarReporte = null) : IReservaEspaciosFacade
{
    public Task<Result<IReadOnlyList<SpaceCatalogoDto>>> ConsultarCatalogo(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default) =>
        consultarCatalogo.HandleAsync(filtro, cancellationToken);

    public Task<Result<DisponibilidadDto>> ConsultarDisponibilidad(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default) =>
        consultarDisponibilidad.HandleAsync(command, cancellationToken);

    public Task<Result<IReadOnlyList<AuditoriaDto>>> ConsultarAuditoria(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken = default) =>
        consultarAuditoria?.HandleAsync(filtro, cancellationToken)
        ?? Task.FromResult(Result<IReadOnlyList<AuditoriaDto>>.Failure(DomainErrors.NoAutorizado));

    public Task<Result<ReporteOcupacionDto>> GenerarReporte(
        FiltroReporte filtro,
        CancellationToken cancellationToken = default) =>
        consultarReporte?.HandleAsync(filtro, cancellationToken)
        ?? Task.FromResult(Result<ReporteOcupacionDto>.Failure(DomainErrors.NoAutorizado));
}
