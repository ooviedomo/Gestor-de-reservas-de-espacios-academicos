namespace ReservaEspacios.Application.Reportes;

/// <summary>
/// Builder (ADR-002): construye el rango del reporte sin constructores con parámetros
/// opcionales. El valor por defecto cubre el mes actual en zona institucional.
/// </summary>
public sealed class ReporteBuilder
{
    private Guid? _spaceId;
    private DateTimeOffset _desde;
    private DateTimeOffset _hasta;

    public ReporteBuilder Desde(DateTimeOffset desde)
    {
        _desde = desde;
        return this;
    }

    public ReporteBuilder Hasta(DateTimeOffset hasta)
    {
        _hasta = hasta;
        return this;
    }

    public ReporteBuilder ParaEspacio(Guid? spaceId)
    {
        _spaceId = spaceId;
        return this;
    }

    public FiltroReporte Build() => new(_spaceId, _desde, _hasta);
}
