using System.Security.Claims;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Web.Security;

/// <summary>
/// Adapter (ADR-002): traduce el ClaimsPrincipal de ASP.NET Core Identity a los
/// códigos de rol del dominio. Sustituye al stub temporal del change anterior.
/// </summary>
public sealed class IdentityRoleAdapter(IHttpContextAccessor accessor) : IUsuarioActual
{
    public Rol Rol => RolExtensions.DesdeCodigo(
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)) ?? Rol.Solicitante;

    public Guid? UsuarioId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
}
