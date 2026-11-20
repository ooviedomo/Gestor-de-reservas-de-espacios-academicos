namespace ReservaEspacios.Application.Auditoria;

/// <summary>Datos de un registro de auditoría ya resueltos para presentación (AUD-001).</summary>
public sealed record AuditoriaDto(
    Guid Id,
    string Entidad,
    Guid EntidadId,
    string Accion,
    Guid? ActorId,
    string? ActorNombre,
    string? ActorRol,
    DateTimeOffset OcurridoEn,
    string? Metadata);

/// <summary>Datos necesarios para insertar un registro de auditoría.</summary>
public sealed record AuditoriaRegistro(
    string Entidad,
    Guid EntidadId,
    string Accion,
    Guid? ActorId,
    string? ActorRol,
    DateTimeOffset OcurridoEn,
    string? Metadata = null);

/// <summary>Filtros opcionales de la consulta de auditoría (AUD-002).</summary>
public sealed record FiltroAuditoria(
    string? Entidad = null,
    Guid? ActorId = null,
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null);
