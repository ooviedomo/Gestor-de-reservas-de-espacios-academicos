using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Template Method (ADR-002): esqueleto fijo `validar actor → verificar transición →
/// aplicar → registrar fecha`. Las clases de estado concretas solo definen qué
/// transiciones aceptan y, si hace falta, una regla adicional de su estado.
/// </summary>
internal abstract class TransicionStateBase : IEstadoReservaState
{
    public abstract EstadoReserva Estado { get; }

    public void Aprobar(Reservation reserva, SolicitudTransicion solicitud) =>
        Ejecutar(reserva, solicitud, TipoTransicion.Aprobar);

    public void Rechazar(Reservation reserva, SolicitudTransicion solicitud) =>
        Ejecutar(reserva, solicitud, TipoTransicion.Rechazar);

    public void Cancelar(Reservation reserva, SolicitudTransicion solicitud) =>
        Ejecutar(reserva, solicitud, TipoTransicion.Cancelar);

    public void Completar(Reservation reserva, SolicitudTransicion solicitud) =>
        Ejecutar(reserva, solicitud, TipoTransicion.Completar);

    private void Ejecutar(Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        ValidarActor(reserva, solicitud, tipo);
        VerificarTransicion(reserva, solicitud, tipo);
        Aplicar(reserva, solicitud, tipo);
        RegistrarFecha(reserva, solicitud, tipo);
        reserva.EstablecerEstado(Destino(tipo));
    }

    private static void ValidarActor(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        var actor = solicitud.Actor;
        var rol = actor.Rol;

        var permitido = tipo switch
        {
            TipoTransicion.Aprobar or TipoTransicion.Rechazar =>
                !actor.EsSistema && rol is not null && rol.Value.PuedeAprobarReserva(),
            TipoTransicion.Completar =>
                actor.EsSistema || (rol is not null && rol.Value.PuedeCompletarReserva()),
            TipoTransicion.Cancelar =>
                !actor.EsSistema && rol is not null &&
                (rol.Value.PuedeCancelarReservaAjena() ||
                 (rol.Value.PuedeCancelarReservaPropia() && actor.UsuarioId == reserva.UsuarioId)),
            _ => false
        };

        if (!permitido)
        {
            throw new DomainException(
                DomainErrors.NoAutorizado, $"El actor no está autorizado para {tipo}.");
        }

        if ((tipo is TipoTransicion.Rechazar or TipoTransicion.Cancelar) &&
            string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            throw new DomainException(
                DomainErrors.MotivoObligatorio, "El motivo es obligatorio.");
        }
    }

    private void VerificarTransicion(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        if (!TransicionPermitida(tipo))
        {
            throw new DomainException(
                DomainErrors.TransicionInvalida,
                $"No se puede {tipo.ToString().ToLowerInvariant()} una reserva en estado {Estado.Codigo()}.");
        }

        ValidarReglaDeEstado(reserva, solicitud, tipo);
    }

    /// <summary>Estado concreto: PENDIENTE acepta aprobar/rechazar/cancelar, APROBADA completar/cancelar, etc.</summary>
    protected abstract bool TransicionPermitida(TipoTransicion tipo);

    /// <summary>Regla adicional del estado, p. ej. vencimiento para el cierre automático.</summary>
    protected virtual void ValidarReglaDeEstado(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
    }

    protected virtual void Aplicar(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        switch (tipo)
        {
            case TipoTransicion.Aprobar:
                reserva.RegistrarAprobacion(solicitud.Actor.UsuarioId);
                break;
            case TipoTransicion.Rechazar:
                reserva.RegistrarRechazo(solicitud.Actor.UsuarioId, solicitud.Motivo!);
                break;
            case TipoTransicion.Cancelar:
                reserva.RegistrarCancelacion(solicitud.Actor.UsuarioId, solicitud.Motivo!);
                break;
            case TipoTransicion.Completar:
                // COM-001 solo exige la marca de tiempo; el actor no es obligatorio.
                break;
        }
    }

    protected virtual void RegistrarFecha(
        Reservation reserva, SolicitudTransicion solicitud, TipoTransicion tipo)
    {
        switch (tipo)
        {
            case TipoTransicion.Aprobar:
                reserva.RegistrarAprobadoEn(solicitud.Momento);
                break;
            case TipoTransicion.Cancelar:
                reserva.RegistrarCanceladoEn(solicitud.Momento);
                break;
            case TipoTransicion.Completar:
                reserva.RegistrarCompletado(solicitud.Momento);
                break;
        }
    }

    private static EstadoReserva Destino(TipoTransicion tipo) => tipo switch
    {
        TipoTransicion.Aprobar => EstadoReserva.Aprobada,
        TipoTransicion.Rechazar => EstadoReserva.Rechazada,
        TipoTransicion.Cancelar => EstadoReserva.Cancelada,
        TipoTransicion.Completar => EstadoReserva.Completada,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null)
    };
}
