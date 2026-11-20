namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Estados del plan §1. Los códigos coinciden con los persistidos en `reservations.estado`
/// y con los expuestos en el historial (RES-004).
/// </summary>
public enum EstadoReserva
{
    Pendiente,
    Aprobada,
    Rechazada,
    Completada,
    Cancelada
}

public static class EstadoReservaExtensions
{
    public static string Codigo(this EstadoReserva estado) => estado switch
    {
        EstadoReserva.Pendiente => "PENDIENTE",
        EstadoReserva.Aprobada => "APROBADA",
        EstadoReserva.Rechazada => "RECHAZADA",
        EstadoReserva.Completada => "COMPLETADA",
        EstadoReserva.Cancelada => "CANCELADA",
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null)
    };

    /// <summary>DISP-002/RES-002: solo PENDIENTE y APROBADA ocupan la franja.</summary>
    public static bool EsBloqueante(this EstadoReserva estado) =>
        estado is EstadoReserva.Pendiente or EstadoReserva.Aprobada;

    public static EstadoReserva? DesdeCodigo(string? codigo) => codigo?.ToUpperInvariant() switch
    {
        "PENDIENTE" => EstadoReserva.Pendiente,
        "APROBADA" => EstadoReserva.Aprobada,
        "RECHAZADA" => EstadoReserva.Rechazada,
        "COMPLETADA" => EstadoReserva.Completada,
        "CANCELADA" => EstadoReserva.Cancelada,
        _ => null
    };
}
