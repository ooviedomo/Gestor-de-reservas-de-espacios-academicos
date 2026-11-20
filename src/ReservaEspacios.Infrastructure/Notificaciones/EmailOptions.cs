namespace ReservaEspacios.Infrastructure.Notificaciones;

/// <summary>
/// Configuración del proveedor SMTP. Se enlaza desde la sección `Correo:Smtp` y admite
/// variables de entorno `Correo__Smtp__*` (ADR-003: sin secretos en el repositorio).
/// </summary>
public sealed class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public string Remitente { get; set; } = "no-responder@reservaespacios.local";
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public bool UsarSsl { get; set; } = true;
}
