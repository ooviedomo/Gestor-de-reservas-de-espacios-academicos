using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Notificaciones;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Notificaciones;

/// <summary>Adapter EF del outbox. Los estados son cadenas estables del spec NOT-001/002.</summary>
public sealed class NotificacionRepository(ReservaEspaciosDbContext context)
    : INotificacionRepository
{
    public async Task AgregarAsync(
        NotificacionPendienteDto notificacion, CancellationToken cancellationToken = default)
    {
        var ahora = DateTimeOffset.UtcNow;
        context.Notificaciones.Add(new Notificacion
        {
            Id = notificacion.Id,
            ReservaId = notificacion.ReservaId,
            Canal = "CORREO",
            Destinatario = notificacion.Destinatario,
            Asunto = notificacion.Asunto,
            Cuerpo = notificacion.Cuerpo,
            Estado = "PENDIENTE",
            Intentos = notificacion.Intentos,
            CreadoEn = ahora,
            ProximoIntentoEn = ahora
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificacionPendienteDto>> ObtenerPendientesAsync(
        DateTimeOffset ahora,
        int limite,
        CancellationToken cancellationToken = default) =>
        await context.Notificaciones
            .Where(n => n.Estado == "PENDIENTE" &&
                        (n.ProximoIntentoEn == null || n.ProximoIntentoEn <= ahora))
            .OrderBy(n => n.CreadoEn)
            .Take(limite)
            .Select(n => new NotificacionPendienteDto(
                n.Id, n.ReservaId, n.Destinatario, n.Asunto, n.Cuerpo, n.Intentos))
            .ToListAsync(cancellationToken);

    public async Task MarcarEnviadaAsync(
        Guid id, DateTimeOffset enviadoEn, CancellationToken cancellationToken = default)
    {
        var notificacion = await context.Notificaciones.FindAsync([id], cancellationToken);
        if (notificacion is null)
        {
            return;
        }

        notificacion.Estado = "ENVIADA";
        notificacion.EnviadoEn = enviadoEn;
        notificacion.UltimoError = null;
        notificacion.ProximoIntentoEn = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RegistrarFalloAsync(
        Guid id,
        int intentos,
        string error,
        DateTimeOffset proximoIntento,
        bool agotado,
        CancellationToken cancellationToken = default)
    {
        var notificacion = await context.Notificaciones.FindAsync([id], cancellationToken);
        if (notificacion is null)
        {
            return;
        }

        notificacion.Intentos = intentos;
        notificacion.UltimoError = error.Length > 2000 ? error[..2000] : error;
        notificacion.ProximoIntentoEn = agotado ? null : proximoIntento;
        notificacion.Estado = agotado ? "ERROR" : "PENDIENTE";
        await context.SaveChangesAsync(cancellationToken);
    }
}
