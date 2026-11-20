using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Web.Security;

/// <summary>
/// Implementación temporal hasta que el siguiente change incorpore ASP.NET Core Identity
/// y el `IdentityRoleAdapter` (ADR-002). Principio de menor privilegio: Solicitante.
/// </summary>
public sealed class UsuarioActualStub : IUsuarioActual
{
    public Rol Rol => Rol.Solicitante;
    public Guid? UsuarioId => Guid.Empty;
}
