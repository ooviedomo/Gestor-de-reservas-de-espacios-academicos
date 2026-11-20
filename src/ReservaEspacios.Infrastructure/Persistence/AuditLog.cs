namespace ReservaEspacios.Infrastructure.Persistence;

/// <summary>Registro append-only de una acción sensible (AUD-001).</summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public Guid EntidadId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorRol { get; set; }
    public DateTimeOffset OcurridoEn { get; set; }
    public string? Metadata { get; set; }
}
