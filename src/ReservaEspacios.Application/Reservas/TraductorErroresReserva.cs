using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Reservas;

/// <summary>
/// Tarea 2.3: convierte la violación de la restricción `no_overlap` en un código
/// estable de dominio. No referencia Npgsql: Infrastructure solo entrega el
/// `SqlState` y el nombre de la restricción, de modo que Application decide el
/// mapeo sin acoplarse al proveedor de persistencia.
/// </summary>
public static class TraductorErroresReserva
{
    public const string SqlStateExclusionViolation = "23P01";
    public const string RestriccionSolape = "no_overlap";

    public static DomainException? Traducir(string? sqlState, string? constraintName) =>
        sqlState == SqlStateExclusionViolation && constraintName == RestriccionSolape
            ? new DomainException(
                DomainErrors.SolapeDetectado,
                "La franja solicitada ya está reservada para ese espacio.")
            : null;
}
