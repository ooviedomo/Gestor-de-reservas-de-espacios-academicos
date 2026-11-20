using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Common;

/// <summary>
/// Puerto de seguridad para conocer a quién ejecuta un caso de uso. Lo implementa
/// `IdentityRoleAdapter` (ADR-002) leyendo el ClaimsPrincipal de ASP.NET Core Identity.
/// </summary>
public interface IUsuarioActual
{
    Rol Rol { get; }
    Guid? UsuarioId { get; }
    bool EstaAutenticado => UsuarioId is not null;
}
