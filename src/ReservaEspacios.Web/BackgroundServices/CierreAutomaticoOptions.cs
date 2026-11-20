namespace ReservaEspacios.Web.BackgroundServices;

/// <summary>
/// Configuración del cierre diario de COM-001. Se enlaza desde
/// <c>Reservas:CierreAutomatico</c>; el valor por defecto respeta el escenario
/// "proceso de cierre diario" de la spec.
/// </summary>
public sealed class CierreAutomaticoOptions
{
    public int IntervaloSegundos { get; set; } = 86_400;
}
