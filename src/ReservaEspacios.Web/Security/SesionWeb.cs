using Microsoft.AspNetCore.Identity;
using ReservaEspacios.Infrastructure.Identity;

namespace ReservaEspacios.Web.Security;

/// <summary>
/// Cierra la cookie de Identity (UI-004). No revalida credenciales: esa responsabilidad
/// sigue en <c>AutenticarUsuario</c> (USR-003).
/// </summary>
public sealed class SesionWeb(SignInManager<ApplicationUser> signInManager) : ISesionWeb
{
    public Task CerrarSesionAsync(CancellationToken cancellationToken = default) =>
        signInManager.SignOutAsync();
}
