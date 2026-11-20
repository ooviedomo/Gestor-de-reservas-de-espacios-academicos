using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>Admite completar (manual o automático) y cancelar; no se puede reaprobar.</summary>
internal sealed class AprobadaState : TransicionStateBase
{
    public static AprobadaState Instancia { get; } = new();

    private AprobadaState()
    {
    }

    public override EstadoReserva Estado => EstadoReserva.Aprobada;

    protected override bool TransicionPermitida(TipoTransicion tipo) =>
        tipo is TipoTransicion.Completar or TipoTransicion.Cancelar;

    /// <summary>COM-001: el cierre automático solo procede cuando <c>fin</c> ya venció.</summary>
    protected override void ValidarReglaDeEstado(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        if (tipo == TipoTransicion.Completar &&
            solicitud.Actor.EsSistema &&
            reserva.Fin > solicitud.Momento)
        {
            throw new DomainException(
                DomainErrors.TransicionInvalida,
                "El completado automático requiere que la fecha fin haya vencido.");
        }
    }
}
