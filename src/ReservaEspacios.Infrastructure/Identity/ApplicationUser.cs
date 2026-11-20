using Microsoft.AspNetCore.Identity;

namespace ReservaEspacios.Infrastructure.Identity;

/// <summary>
/// Usuario de ASP.NET Core Identity. El email se usa también como nombre de usuario
/// (USR-001/USR-003); <see cref="Activo"/> implementa la desactivación de USR-002.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
