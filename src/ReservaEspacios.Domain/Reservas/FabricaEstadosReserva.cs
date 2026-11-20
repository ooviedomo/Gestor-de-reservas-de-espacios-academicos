namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Selecciona el State concreto a partir del estado persistido (ADR-002).
/// Las instancias son inmutables y se comparten.
/// </summary>
internal static class FabricaEstadosReserva
{
    public static IEstadoReservaState Obtener(EstadoReserva estado) => estado switch
    {
        EstadoReserva.Pendiente => PendienteState.Instancia,
        EstadoReserva.Aprobada => AprobadaState.Instancia,
        EstadoReserva.Rechazada => RechazadaState.Instancia,
        EstadoReserva.Completada => CompletadaState.Instancia,
        EstadoReserva.Cancelada => CanceladaState.Instancia,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null)
    };
}
