using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Espacios;

public sealed record SpaceCatalogoDto(
    Guid Id,
    string Nombre,
    EspacioTipo Tipo,
    int Capacidad,
    string Ubicacion,
    bool Activo);

/// <summary>ESP-003: catálogo filtrable con guard de rol sobre el filtro de estado.</summary>
public sealed class ConsultarCatalogo(ISpaceRepository repository, IUsuarioActual usuarioActual)
{
    public async Task<Result<IReadOnlyList<SpaceCatalogoDto>>> HandleAsync(
        FiltroCatalogo filtro,
        CancellationToken cancellationToken = default)
    {
        var estado = usuarioActual.Rol.PuedeVerEspaciosInactivos()
            ? filtro.Estado
            : EstadoSpaceFiltro.Activo;

        var filtroEfectivo = filtro with { Estado = estado };
        var espacios = await repository.ListAsync(filtroEfectivo, cancellationToken);

        return Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
            espacios.Select(x => new SpaceCatalogoDto(
                x.Id, x.Nombre, x.Tipo, x.Capacidad, x.Ubicacion, x.Activo)).ToList());
    }
}
