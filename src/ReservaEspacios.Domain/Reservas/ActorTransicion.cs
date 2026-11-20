using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Domain.Reservas;

/// <summary>
/// Actor de una transición de reserva (APR-001, RES-003, COM-001).
/// <see cref="Sistema"/> representa el cierre automático, que no tiene usuario ni rol.
/// </summary>
public sealed record ActorTransicion(Guid? UsuarioId, Rol? Rol)
{
    public static ActorTransicion Sistema { get; } = new(null, null);

    public static ActorTransicion Usuario(Guid usuarioId, Rol rol) => new(usuarioId, rol);

    public bool EsSistema => UsuarioId is null && Rol is null;
}

/// <summary>Datos de una solicitud de cambio de estado; <c>Ahora</c> permite probar sin depender del reloj.</summary>
public sealed record SolicitudTransicion(
    ActorTransicion Actor,
    string? Motivo = null,
    DateTimeOffset? Ahora = null)
{
    public DateTimeOffset Momento => (Ahora ?? DateTimeOffset.UtcNow).ToUniversalTime();
}
