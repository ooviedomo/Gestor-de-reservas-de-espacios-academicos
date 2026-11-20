namespace ReservaEspacios.Web.BackgroundServices;

/// <summary>
/// Intervalo del worker de notificaciones, enlazado desde `Notificaciones` (segundos).
/// El valor por defecto evita consumir CPU en entornos sin configuración.
/// </summary>
public sealed class NotificacionesOptions
{
    public int IntervaloSegundos { get; set; } = 30;
    public int Lote { get; set; } = 20;
}
