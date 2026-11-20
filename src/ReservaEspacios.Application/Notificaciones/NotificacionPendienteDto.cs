namespace ReservaEspacios.Application.Notificaciones;

/// <summary>Proyección del outbox que Application necesita para programar un envío.</summary>
public sealed record NotificacionPendienteDto(
    Guid Id,
    Guid ReservaId,
    string Destinatario,
    string Asunto,
    string Cuerpo,
    int Intentos);
