using Npgsql;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Infrastructure.Reservas;

/// <summary>
/// Puente Infrastructure → Application (ADR-002):extrae el <c>SqlState</c> de la
/// excepción de Npgsql —incluso cuando EF Core la envuelve en <c>DbUpdateException</c>—
/// y delega el mapeo en <see cref="TraductorErroresReserva"/>, que no conoce al
/// proveedor. Lo usan tanto el decorator del repositorio como la transacción, de modo
/// que un fallo de serialización en el <c>CommitAsync</c> también llegue traducido.
/// </summary>
public static class TraductorPostgresReserva
{
    public static DomainException? Traducir(Exception exception)
    {
        var postgres = ExtraerPostgresException(exception);
        return postgres is null
            ? null
            : TraductorErroresReserva.Traducir(postgres.SqlState, postgres.ConstraintName);
    }

    private static PostgresException? ExtraerPostgresException(Exception exception)
    {
        var actual = exception;
        while (actual is not null)
        {
            if (actual is PostgresException postgres)
            {
                return postgres;
            }

            actual = actual.InnerException;
        }

        return null;
    }
}
