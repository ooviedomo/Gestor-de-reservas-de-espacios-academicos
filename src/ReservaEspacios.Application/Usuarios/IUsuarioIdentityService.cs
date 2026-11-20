using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Usuarios;

/// <summary>Datos de la sesión devueltos por USR-003.</summary>
public sealed record UsuarioSesionDto(
    Guid Id,
    string Nombre,
    string Email,
    Rol Rol,
    bool Activo);

/// <summary>
/// RES-004: identidad mínima para mostrar solicitante y actor en el historial. El email
/// se añade sin romper las construcciones existentes para resolver el destino del outbox.
/// </summary>
public sealed record DatosActorDto(Guid Id, string Nombre, Rol Rol, string Email = "");

public sealed record CrearUsuarioCommand(
    string Nombre,
    string Email,
    string Contrasena,
    Rol Rol);

/// <summary>
/// Puerto sobre ASP.NET Core Identity. Aísla Application del proveedor de identidad,
/// de modo que los casos de uso solo conocen códigos de error del dominio.
/// </summary>
public interface IUsuarioIdentityService
{
    Task<Result<Guid>> CrearAsync(
        CrearUsuarioCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<UsuarioSesionDto>> AutenticarAsync(
        string email,
        string contrasena,
        CancellationToken cancellationToken = default);

    Task<Result> CambiarRolAsync(
        Guid usuarioId,
        Rol rol,
        CancellationToken cancellationToken = default);

    Task<Result> ActivarDesactivarAsync(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken = default);

    Task<Result<UsuarioSesionDto?>> BuscarPorEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>RES-004: nombre y rol de un usuario, para resolver los actores del historial.</summary>
    Task<Result<DatosActorDto?>> ObtenerPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
