namespace ReservaEspacios.Application.Notificaciones;

/// <summary>
/// Strategy (ADR-002): política de reintentos del worker. El límite es de 5 intentos
/// según NOT-002; la implementación concreta calcula el retroceso exponencial.
/// </summary>
public interface IRetryBackoff
{
    int MaxIntentos { get; }

    TimeSpan Calcular(int intentos);
}

public sealed class BackoffExponencial : IRetryBackoff
{
    public int MaxIntentos => 5;

    public TimeSpan Calcular(int intentos)
    {
        var exponente = Math.Clamp(intentos, 0, 10);
        return TimeSpan.FromSeconds(Math.Min(3600, Math.Pow(2, exponente)));
    }
}
