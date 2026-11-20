using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Observer (ADR-002): eventos publicados después de persistir una transición de reserva.
/// No sustituyen al historial de `Reservation`: son la señal para auditoría y outbox del
/// tercer change. El actor es anulable porque el cierre automático no tiene usuario.
/// </summary>
public sealed record ReservaAprobada(
    Guid ReservaId,
    Guid EspacioId,
    Guid SolicitanteId,
    Guid? ActorId,
    Rol? ActorRol,
    DateTimeOffset AprobadoEn,
    DateTimeOffset OcurridoEn) : IDomainEvent;

public sealed record ReservaRechazada(
    Guid ReservaId,
    Guid EspacioId,
    Guid SolicitanteId,
    Guid? ActorId,
    Rol? ActorRol,
    string Motivo,
    DateTimeOffset OcurridoEn) : IDomainEvent;

public sealed record ReservaCancelada(
    Guid ReservaId,
    Guid EspacioId,
    Guid SolicitanteId,
    Guid? ActorId,
    Rol? ActorRol,
    string Motivo,
    DateTimeOffset OcurridoEn) : IDomainEvent;

public sealed record ReservaCompletada(
    Guid ReservaId,
    Guid EspacioId,
    Guid SolicitanteId,
    Guid? ActorId,
    Rol? ActorRol,
    DateTimeOffset CompletadoEn,
    bool Automatico,
    DateTimeOffset OcurridoEn) : IDomainEvent;
