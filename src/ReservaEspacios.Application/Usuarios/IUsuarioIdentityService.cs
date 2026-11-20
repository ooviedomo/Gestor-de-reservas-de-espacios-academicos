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
}
