namespace ReservaEspacios.Domain.Reservas;

/// <summary>Estado terminal (APR-001): ninguna acción lo modifica.</summary>
internal sealed class RechazadaState : TransicionStateBase
{
    public static RechazadaState Instancia { get; } = new();

    private RechazadaState()
    {
    }

    public override EstadoReserva Estado => EstadoReserva.Rechazada;

    protected override bool TransicionPermitida(TipoTransicion tipo) => false;
}
