namespace ReservaEspacios.Infrastructure.Persistence;

/// <summary>Outbox de correo: la reserva y este registro se confirman juntos (NOT-001/003).</summary>
public sealed class Notificacion
{
    public Guid Id { get; set; }
    public Guid ReservaId { get; set; }
    public string Canal { get; set; } = "CORREO";
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
    public DateTimeOffset? EnviadoEn { get; set; }
    public DateTimeOffset? ProximoIntentoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
}
