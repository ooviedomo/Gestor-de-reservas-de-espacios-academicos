using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Solicitud de reserva (RES-001 a RES-004). El intervalo es semiabierto `[inicio, fin)`;
/// la restricción de exclusión de PostgreSQL es la última barrera anti-solapes.
/// Las transiciones se implementan en la tarea 2.2 con el patrón State.
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
}
