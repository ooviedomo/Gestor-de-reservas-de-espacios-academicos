namespace ReservaEspacios.Domain.Usuarios;

/// <summary>
/// Códigos de rol del plan §1. ASP.NET Core Identity se incorporará en el siguiente
/// change; `IdentityRoleAdapter` traducirá sus roles a estos valores (ADR-002).
/// </summary>
public enum Rol
{
    Solicitante,
    Coordinador,
    Administrador
}

public static class RolExtensions
{
    /// <summary>ESP-003: solo gestión puede incluir espacios inactivos en el catálogo.</summary>
    public static bool PuedeVerEspaciosInactivos(this Rol rol) =>
        rol is Rol.Coordinador or Rol.Administrador;
}
