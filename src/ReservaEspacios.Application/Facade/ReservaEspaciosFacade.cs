using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;

namespace ReservaEspacios.Application.Facade;

public sealed class ReservaEspaciosFacade(
    IConsultarCatalogo consultarCatalogo,
    IConsultarDisponibilidad consultarDisponibilidad) : IReservaEspaciosFacade
{
    public Task<Result<IReadOnlyList<SpaceCatalogoDto>>> ConsultarCatalogo(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default) =>
        consultarCatalogo.HandleAsync(filtro, cancellationToken);

    public Task<Result<DisponibilidadDto>> ConsultarDisponibilidad(
        ConsultarDisponibilidadCommand command,
        CancellationToken cancellationToken = default) =>
        consultarDisponibilidad.HandleAsync(command, cancellationToken);
}
