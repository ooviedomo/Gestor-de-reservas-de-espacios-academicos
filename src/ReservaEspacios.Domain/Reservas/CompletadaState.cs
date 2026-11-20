namespace ReservaEspacios.Domain.Reservas;

/// <summary>Estado terminal (COM-001): ninguna acción lo modifica.</summary>
internal sealed class CompletadaState : TransicionStateBase
{
    public static CompletadaState Instancia { get; } = new();

    private CompletadaState()
    {
    }

    public override EstadoReserva Estado => EstadoReserva.Completada;

    protected override bool TransicionPermitida(TipoTransicion tipo) => false;
}
