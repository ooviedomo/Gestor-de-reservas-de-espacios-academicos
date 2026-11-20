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

    /// <summary>Código persistido en la tabla `roles` (plan §1 y §3.3).</summary>
    public static string Codigo(this Rol rol) => rol switch
    {
        Rol.Solicitante => "SOLICITANTE",
        Rol.Coordinador => "COORDINADOR",
        Rol.Administrador => "ADMINISTRADOR",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, null)
    };

    /// <summary>Etiqueta legible mostrada en la UI y en el historial (RES-004).</summary>
    public static string Etiqueta(this Rol rol) => rol switch
    {
        Rol.Solicitante => "Solicitante",
        Rol.Coordinador => "Coordinador de espacios",
        Rol.Administrador => "Administrador del sistema",
        _ => throw new ArgumentOutOfRangeException(nameof(rol), rol, null)
    };

    /// <summary>Traducción inversa usada por `IdentityRoleAdapter` (ADR-002).</summary>
    public static Rol? DesdeCodigo(string? codigo) => codigo?.ToUpperInvariant() switch
    {
        "SOLICITANTE" => Rol.Solicitante,
        "COORDINADOR" => Rol.Coordinador,
        "ADMINISTRADOR" => Rol.Administrador,
        _ => null
    };
}
