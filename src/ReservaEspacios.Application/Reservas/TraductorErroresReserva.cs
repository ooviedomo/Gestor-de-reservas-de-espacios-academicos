using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// Convierte errores de persistencia de reservas en códigos estables de dominio.
/// No referencia Npgsql: Infrastructure solo entrega el <c>SqlState</c> y el nombre
/// de la restricción, de modo que Application decide el mapeo sin acoplarse al
/// proveedor (ADR-002).
/// <list type="bullet">
/// <item><c>23P01</c>/<c>no_overlap</c>: la restricción de exclusión detectó el solape.</item>
/// <item><c>40001</c>: bajo <c>SERIALIZABLE</c>, otra transacción ocupó la franja
/// mientras se confirmaba (tarea 3.3).</item>
/// </list>
/// </summary>
public static class TraductorErroresReserva
{
    public const string SqlStateExclusionViolation = "23P01";
    public const string SqlStateSerializationFailure = "40001";
    public const string RestriccionSolape = "no_overlap";

    private const string MensajeSolape =
        "La franja solicitada ya está reservada para ese espacio.";

    public static DomainException? Traducir(string? sqlState, string? constraintName)
    {
        var esSolape =
            (sqlState == SqlStateExclusionViolation && constraintName == RestriccionSolape) ||
            sqlState == SqlStateSerializationFailure;

        return esSolape
            ? new DomainException(DomainErrors.SolapeDetectado, MensajeSolape)
            : null;
    }
}
