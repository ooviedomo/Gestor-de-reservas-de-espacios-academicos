using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Solicitud de reserva (RES-001 a RES-004). El intervalo es semiabierto `[inicio, fin)`;
/// la restricción de exclusión de PostgreSQL es la última barrera anti-solapes.
/// Las transiciones se delegan en el State concreto (ADR-002).
/// </summary>
public sealed class Reservation
{
    private Reservation()
    {
    }

    public Reservation(
        Guid id,
        Guid spaceId,
        Guid usuarioId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        string proposito,
        int asistentes,
        DateTimeOffset? creadoEn = null)
    {
        if (fin <= inicio)
        {
            throw new DomainException(
                DomainErrors.RangoInvalido, "El inicio debe ser anterior al fin.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(proposito);

        Id = id;
        SpaceId = spaceId;
        UsuarioId = usuarioId;
        Inicio = inicio.ToUniversalTime();
        Fin = fin.ToUniversalTime();
        Proposito = proposito.Trim();
        Asistentes = asistentes;
        Estado = EstadoReserva.Pendiente;
        CreadoEn = (creadoEn ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset Fin { get; private set; }
    public string Proposito { get; private set; } = string.Empty;
    public int Asistentes { get; private set; }
    public EstadoReserva Estado { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    /// <summary>Actor que aprobó, rechazó o canceló; se muestra con nombre y rol en RES-004.</summary>
    public Guid? AprobadoPor { get; private set; }
    public DateTimeOffset? AprobadoEn { get; private set; }
    public Guid? ActorId { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public string? MotivoCancelacion { get; private set; }
    public DateTimeOffset? CanceladoEn { get; private set; }
    public DateTimeOffset? CompletadoEn { get; private set; }

    public bool EsBloqueante => Estado.EsBloqueante();

    private IEstadoReservaState EstadoActual => FabricaEstadosReserva.Obtener(Estado);

    /// <summary>APR-001: PENDIENTE → APROBADA, con actor y fecha.</summary>
    public void Aprobar(ActorTransicion actor, DateTimeOffset? ahora = null) =>
        EstadoActual.Aprobar(this, new SolicitudTransicion(actor, Ahora: ahora));

    /// <summary>APR-001: PENDIENTE → RECHAZADA, con motivo obligatorio.</summary>
    public void Rechazar(ActorTransicion actor, string motivo, DateTimeOffset? ahora = null) =>
        EstadoActual.Rechazar(this, new SolicitudTransicion(actor, motivo, ahora));

    /// <summary>RES-003: PENDIENTE/APROBADA → CANCELADA, con motivo obligatorio.</summary>
    public void Cancelar(ActorTransicion actor, string motivo, DateTimeOffset? ahora = null) =>
        EstadoActual.Cancelar(this, new SolicitudTransicion(actor, motivo, ahora));

    /// <summary>COM-001: APROBADA → COMPLETADA, manualmente o desde el cierre automático.</summary>
    public void Completar(ActorTransicion actor, DateTimeOffset? ahora = null) =>
        EstadoActual.Completar(this, new SolicitudTransicion(actor, Ahora: ahora));

    public void CompletarAutomaticamente(DateTimeOffset? ahora = null) =>
        Completar(ActorTransicion.Sistema, ahora);

    // Los States aplican los cambios a través de estos métodos internal para que
    // las propiedades sigan siendo de solo lectura desde fuera de la entidad.
    internal void EstablecerEstado(EstadoReserva estado) => Estado = estado;

    internal void RegistrarAprobacion(Guid? actorId)
    {
        AprobadoPor = actorId;
        ActorId = actorId;
    }

    internal void RegistrarAprobadoEn(DateTimeOffset en) => AprobadoEn = en.ToUniversalTime();

    internal void RegistrarRechazo(Guid? actorId, string motivo)
    {
        ActorId = actorId;
        MotivoRechazo = motivo.Trim();
    }

    internal void RegistrarCancelacion(Guid? actorId, string motivo)
    {
        ActorId = actorId;
        MotivoCancelacion = motivo.Trim();
    }

    internal void RegistrarCanceladoEn(DateTimeOffset en) => CanceladoEn = en.ToUniversalTime();

    internal void RegistrarCompletado(DateTimeOffset en) => CompletadoEn = en.ToUniversalTime();
}
