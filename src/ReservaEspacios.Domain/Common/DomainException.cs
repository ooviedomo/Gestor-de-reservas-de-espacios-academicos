namespace ReservaEspacios.Domain.Common;

/// <summary>
/// Error de regla de negocio. <see cref="Code"/> se propaga a la API y a la UI.
/// </summary>
public sealed class DomainException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
