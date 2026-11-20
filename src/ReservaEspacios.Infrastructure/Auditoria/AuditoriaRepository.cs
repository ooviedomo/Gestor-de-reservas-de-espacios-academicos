using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Persistence;

namespace ReservaEspacios.Infrastructure.Auditoria;

/// <summary>
/// Adapter de <see cref="IAuditoriaRepository"/> sobre EF Core. Solo inserta y consulta;
/// el borrado se intenta únicamente desde el comando de verificación de inmutabilidad
/// y siempre es rechazado por el trigger <c>audit_logs_inmutable</c>.
/// </summary>
public sealed class AuditoriaRepository(ReservaEspaciosDbContext context) : IAuditoriaRepository
{
    public async Task AppendAsync(
        AuditoriaRegistro registro, CancellationToken cancellationToken = default)
    {
        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Entidad = registro.Entidad,
            EntidadId = registro.EntidadId,
            Accion = registro.Accion,
            ActorId = registro.ActorId,
            ActorRol = registro.ActorRol,
            OcurridoEn = registro.OcurridoEn,
            Metadata = registro.Metadata
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditoriaDto>> ConsultarAsync(
        FiltroAuditoria filtro, CancellationToken cancellationToken = default)
    {
        var consulta = from registro in context.AuditLogs
                       join usuario in context.Users
                         on registro.ActorId equals usuario.Id into actores
                       from actor in actores.DefaultIfEmpty()
                       select new
                       {
                           registro.Id,
                           registro.Entidad,
                           registro.EntidadId,
                           registro.Accion,
                           registro.ActorId,
                           ActorNombre = actor == null ? null : actor.Nombre,
                           registro.ActorRol,
                           registro.OcurridoEn,
                           registro.Metadata
                       };

        if (filtro.Entidad is { Length: > 0 } entidad)
        {
            consulta = consulta.Where(x => x.Entidad == entidad);
        }

        if (filtro.ActorId is { } actorId)
        {
            consulta = consulta.Where(x => x.ActorId == actorId);
        }

        if (filtro.Desde is { } desde)
        {
            consulta = consulta.Where(x => x.OcurridoEn >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            consulta = consulta.Where(x => x.OcurridoEn <= hasta);
        }

        var filas = await consulta
            .OrderByDescending(x => x.OcurridoEn)
            .ToListAsync(cancellationToken);

        return filas.Select(fila => new AuditoriaDto(
            fila.Id,
            fila.Entidad,
            fila.EntidadId,
            fila.Accion,
            fila.ActorId,
            fila.ActorNombre,
            RolExtensions.DesdeCodigo(fila.ActorRol)?.Etiqueta(),
            fila.OcurridoEn,
            fila.Metadata)).ToList();
    }

    public async Task<Result> IntentarEliminarAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var registro = await context.AuditLogs.FindAsync([id], cancellationToken);
        if (registro is null)
        {
            return Result.Failure(DomainErrors.AuditoriaInmutable);
        }

        context.AuditLogs.Remove(registro);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { Message: { } mensaje } &&
            mensaje.Contains(DomainErrors.AuditoriaInmutable, StringComparison.Ordinal))
        {
            context.Entry(registro).State = EntityState.Detached;
            return Result.Failure(DomainErrors.AuditoriaInmutable);
        }

        // Si el trigger no estuviera instalado, la operación no debe parecer exitosa.
        context.Entry(registro).State = EntityState.Detached;
        return Result.Failure(DomainErrors.AuditoriaInmutable);
    }
}
