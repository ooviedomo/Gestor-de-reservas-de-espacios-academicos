using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Auditoria;

/// <summary>
/// Observer (ADR-002): reacciona a los eventos publicados por los casos de uso y escribe
/// el registro append-only. No decide permisos ni reintentos: solo traduce evento a fila.
/// El rol del actor se persiste como código canónico y se muestra como etiqueta al consultar.
/// </summary>
public sealed class AuditoriaHandler(IAuditoriaRepository repository) : IDomainEventSubscriber
{
    public Task HandleAsync(IDomainEvent evento, CancellationToken cancellationToken = default)
    {
        var registro = evento switch
        {
            ReservaCreada creada => new AuditoriaRegistro(
                "Reservation", creada.ReservaId, "CREADA", creada.ActorId,
                Codigo(creada.ActorRol), creada.OcurridoEn,
                Metadata("inicio", creada.Inicio, "fin", creada.Fin)),
            ReservaAprobada aprobada => new AuditoriaRegistro(
                "Reservation", aprobada.ReservaId, "APROBADA", aprobada.ActorId,
                Codigo(aprobada.ActorRol), aprobada.OcurridoEn),
            ReservaRechazada rechazada => new AuditoriaRegistro(
                "Reservation", rechazada.ReservaId, "RECHAZADA", rechazada.ActorId,
                Codigo(rechazada.ActorRol), rechazada.OcurridoEn,
                Metadata("motivo", rechazada.Motivo)),
            ReservaCancelada cancelada => new AuditoriaRegistro(
                "Reservation", cancelada.ReservaId, "CANCELADA", cancelada.ActorId,
                Codigo(cancelada.ActorRol), cancelada.OcurridoEn,
                Metadata("motivo", cancelada.Motivo)),
            ReservaCompletada completada => new AuditoriaRegistro(
                "Reservation", completada.ReservaId, "COMPLETADA", completada.ActorId,
                Codigo(completada.ActorRol), completada.OcurridoEn,
                Metadata("automatico", completada.Automatico)),
            UsuarioCreado creado => new AuditoriaRegistro(
                "User", creado.UsuarioId, "CREADO", creado.ActorId,
                Codigo(creado.ActorRol), creado.OcurridoEn,
                Metadata("email", creado.Email)),
            RolCambiado cambiado => new AuditoriaRegistro(
                "User", cambiado.UsuarioId, "ROL_CAMBIADO", cambiado.ActorId,
                Codigo(cambiado.ActorRol), cambiado.OcurridoEn,
                Metadata("rol", cambiado.RolNuevo.Codigo())),
            _ => null
        };

        return registro is null
            ? Task.CompletedTask
            : repository.AppendAsync(registro, cancellationToken);
    }

    private static string? Codigo(Rol? rol) => rol?.Codigo();

    private static string Metadata(params object?[] pares)
    {
        var datos = new Dictionary<string, object?>();
        for (var i = 0; i + 1 < pares.Length; i += 2)
        {
            datos[pares[i]!.ToString()!] = pares[i + 1];
        }

        // UnicodeRanges.All conserva caracteres como «ó» legibles en la vista de
        // auditoría. Razor codifica el HTML al representarlo, por lo que no hay XSS.
        return JsonSerializer.Serialize(datos, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
    }
}
