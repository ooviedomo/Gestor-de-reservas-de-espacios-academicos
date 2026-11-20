namespace ReservaEspacios.Domain.Reservas;

/// <summary>Estado terminal (RES-003): ninguna acción lo modifica y no bloquea la franja.</summary>
internal sealed class CanceladaState : TransicionStateBase
{
    public static CanceladaState Instancia { get; } = new();

    private CanceladaState()
    {
    }

    public override EstadoReserva Estado => EstadoReserva.Cancelada;

    protected override bool TransicionPermitida(TipoTransicion tipo) => false;
}
