using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Usuarios;

/// <summary>
/// Observer (ADR-002): eventos de acciones sensibles sobre cuentas (AUD-001). El actor
/// es nulo en el autorregistro, porque todavía no existe una sesión autenticada.
/// </summary>
public sealed record UsuarioCreado(
    Guid UsuarioId,
    string Nombre,
    string Email,
    Rol Rol,
    Guid? ActorId,
    Rol? ActorRol,
    DateTimeOffset OcurridoEn) : IDomainEvent;

public sealed record RolCambiado(
    Guid UsuarioId,
    Rol RolNuevo,
    Guid? ActorId,
    Rol? ActorRol,
    DateTimeOffset OcurridoEn) : IDomainEvent;
