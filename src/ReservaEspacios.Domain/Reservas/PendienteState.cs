namespace ReservaEspacios.Domain.Reservas;

/// <summary>Estado inicial (RES-001): admite aprobar, rechazar y cancelar.</summary>
internal sealed class PendienteState : TransicionStateBase
{
    public static PendienteState Instancia { get; } = new();

    private PendienteState()
    {
    }

    public override EstadoReserva Estado => EstadoReserva.Pendiente;

    protected override bool TransicionPermitida(TipoTransicion tipo) =>
        tipo is TipoTransicion.Aprobar or TipoTransicion.Rechazar or TipoTransicion.Cancelar;
}
