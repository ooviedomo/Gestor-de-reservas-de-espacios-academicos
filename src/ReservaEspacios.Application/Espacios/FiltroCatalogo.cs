using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Espacios;

/// <summary>ESP-003: estado solicitado para el catálogo.</summary>
public enum EstadoSpaceFiltro
{
    Activo,
    Inactivo,
    Todos
}

public sealed record FiltroCatalogo(
    EspacioTipo? Tipo,
    int? CapacidadMinima,
    EstadoSpaceFiltro Estado);
