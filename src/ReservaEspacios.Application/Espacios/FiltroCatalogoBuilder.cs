using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Espacios;

/// <summary>
/// Patrón Builder (ADR-002): evita constructores con múltiples parámetros opcionales
/// y deja explícito que el catálogo solo muestra activos por defecto (ESP-003).
/// </summary>
public sealed class FiltroCatalogoBuilder
{
    private EspacioTipo? _tipo;
    private int? _capacidadMinima;
    private EstadoSpaceFiltro _estado = EstadoSpaceFiltro.Activo;

    public FiltroCatalogoBuilder ConTipo(EspacioTipo? tipo)
    {
        _tipo = tipo;
        return this;
    }

    public FiltroCatalogoBuilder ConCapacidadMinima(int? capacidadMinima)
    {
        _capacidadMinima = capacidadMinima;
        return this;
    }

    public FiltroCatalogoBuilder ConEstado(EstadoSpaceFiltro estado)
    {
        _estado = estado;
        return this;
    }

    public FiltroCatalogo Build() => new(_tipo, _capacidadMinima, _estado);
}
