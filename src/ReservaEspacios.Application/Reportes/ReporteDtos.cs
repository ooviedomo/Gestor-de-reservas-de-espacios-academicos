namespace ReservaEspacios.Application.Reportes;

/// <summary>Rango y filtro opcional de espacio construidos por <see cref="ReporteBuilder"/>.</summary>
public sealed record FiltroReporte(Guid? SpaceId, DateTimeOffset Desde, DateTimeOffset Hasta);

public sealed record IntervaloReserva(DateTimeOffset Inicio, DateTimeOffset Fin);

/// <summary>Datos ya agrupados por espacio que el repositorio entrega al Strategy.</summary>
public sealed record DatosOcupacionEspacio(
    Guid SpaceId,
    string Nombre,
    TimeOnly Apertura,
    TimeOnly Cierre,
    int NumeroReservas,
    IReadOnlyList<IntervaloReserva> Intervalos);

public sealed record DetalleOcupacionDto(
    Guid EspacioId,
    string Espacio,
    int Reservas,
    double HorasReservadas,
    double HorasDisponibles,
    double PorcentajeOcupacion);

public sealed record ReporteOcupacionDto(
    IReadOnlyList<DetalleOcupacionDto> Espacios,
    DateTimeOffset Desde,
    DateTimeOffset Hasta);
