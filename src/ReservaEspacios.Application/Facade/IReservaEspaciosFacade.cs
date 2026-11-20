using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;

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
}
