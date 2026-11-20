using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Web;

/// <summary>
/// Endpoints HTTP del change. Las reglas de autorización viven en
/// <see cref="IEvaluadorPermisos"/> y en los guards de Application, no aquí.
/// Los endpoints todavía no implementados devuelven 501 después de pasar el guard,
/// para que la matriz de AUTH-001/002/003 sea verificable desde el inicio.
/// </summary>
public static class ApiEndpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/autenticacion/registro", async (
            RegistrarSolicitanteCommand command,
            RegistrarSolicitante registrar) =>
        {
            var resultado = await registrar.HandleAsync(command);
            return resultado.IsSuccess
                ? Results.Ok(new { id = resultado.Value })
                : Error(resultado.ErrorCode);
        });

        app.MapPost("/api/autenticacion/login", async (
            AutenticarUsuarioCommand command,
            AutenticarUsuario autenticar) =>
        {
            var resultado = await autenticar.HandleAsync(command);
            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Error(resultado.ErrorCode);
        });

        // Consulta de permisos para la UI: evalúa la misma Strategy que usan los guards.
        app.MapGet("/api/autorizacion/permisos", (
            IEvaluadorPermisos permisos,
            IUsuarioActual actual) =>
        {
            if (!actual.EstaAutenticado)
            {
                return Error(DomainErrors.NoAutenticado);
            }

            var rol = actual.Rol;
            return Results.Ok(new
            {
                crearReserva = permisos.PuedeCrearReserva(rol),
                cancelarReservaPropia = permisos.PuedeCancelarReserva(rol, Guid.Empty, Guid.Empty),
                verHistorialCompleto = permisos.PuedeVerHistorialCompleto(rol),
                aprobar = permisos.PuedeAprobar(rol),
                completar = permisos.PuedeCompletar(rol),
                administrarEspacios = permisos.PuedeAdministrarEspacios(rol),
                administrarUsuarios = permisos.PuedeAdministrarUsuarios(rol),
                consultarAuditoria = permisos.PuedeConsultarAuditoria(rol),
                consultarReportes = permisos.PuedeConsultarReportes(rol)
            });
        });

        app.MapPost("/api/espacios", async (
            RegistrarSpaceCommand command,
            IRegistrarSpace registrar) =>
        {
            var resultado = await registrar.HandleAsync(command);
            return resultado.IsSuccess
                ? Results.Ok(new { id = resultado.Value })
                : Error(resultado.ErrorCode);
        });

        app.MapPost("/api/usuarios", async (
            CrearUsuarioPrivilegiadoCommand command,
            CrearUsuarioPrivilegiado crear) =>
        {
            var resultado = await crear.HandleAsync(command);
            return resultado.IsSuccess
                ? Results.Ok(new { id = resultado.Value })
                : Error(resultado.ErrorCode);
        });

        app.MapGet("/api/reservas/historial", (
            GuardiaOperacionesProtegidas guardia) =>
        {
            var verificacion = guardia.VerificarHistorial();
            return !verificacion.IsSuccess
                ? Error(verificacion.ErrorCode)
                : NotImplemented("El historial se incorpora en la tarea 4.3.");
        });

        app.MapPost("/api/reservas", async (
            CrearReservaCommand command,
            CrearReserva crear) =>
        {
            var resultado = await crear.HandleAsync(command);
            return resultado.IsSuccess
                ? Results.Created($"/api/reservas/{resultado.Value}", new { id = resultado.Value })
                : Error(resultado.ErrorCode);
        });

        app.MapPost("/api/reservas/{reservaId:guid}/cancelacion", async (
            Guid reservaId,
            CancelarReservaRequest body,
            CancelarReserva cancelar) =>
        {
            var resultado = await cancelar.HandleAsync(
                new CancelarReservaCommand(reservaId, body.Motivo));
            return resultado.IsSuccess
                ? Results.Ok(new { id = reservaId })
                : Error(resultado.ErrorCode);
        });

        app.MapPost("/api/reservas/{reservaId:guid}/aprobacion", async (
            Guid reservaId,
            AprobarReserva aprobar) =>
        {
            var resultado = await aprobar.HandleAsync(new AprobarReservaCommand(reservaId));
            return resultado.IsSuccess
                ? Results.Ok(new { id = reservaId })
                : Error(resultado.ErrorCode);
        });

        app.MapPost("/api/reservas/{reservaId:guid}/rechazo", async (
            Guid reservaId,
            RechazarReservaRequest body,
            RechazarReserva rechazar) =>
        {
            var resultado = await rechazar.HandleAsync(
                new RechazarReservaCommand(reservaId, body.Motivo));
            return resultado.IsSuccess
                ? Results.Ok(new { id = reservaId })
                : Error(resultado.ErrorCode);
        });

        app.MapGet("/api/auditoria", (GuardiaOperacionesProtegidas guardia) =>
        {
            var verificacion = guardia.VerificarAuditoria();
            return !verificacion.IsSuccess
                ? Error(verificacion.ErrorCode)
                : NotImplemented("La auditoría se incorpora en el siguiente change.");
        });

        app.MapGet("/api/reportes/ocupacion", (GuardiaOperacionesProtegidas guardia) =>
        {
            var verificacion = guardia.VerificarReportes();
            return !verificacion.IsSuccess
                ? Error(verificacion.ErrorCode)
                : NotImplemented("Los reportes se incorporan en el siguiente change.");
        });

        return app;
    }

    private static IResult Error(string? codigo) => Results.Json(
        new { error = codigo },
        statusCode: codigo switch
        {
            DomainErrors.NoAutenticado => StatusCodes.Status401Unauthorized,
            DomainErrors.NoAutorizado => StatusCodes.Status403Forbidden,
            DomainErrors.SolapeDetectado => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        });

    private static IResult NotImplemented(string mensaje) => Results.Json(
        new { mensaje },
        statusCode: StatusCodes.Status501NotImplemented);
}

/// <summary>Cuerpo de la cancelación: el identificador viaja en la ruta (tarea 3.4).</summary>
public sealed record CancelarReservaRequest(string Motivo);

/// <summary>Cuerpo del rechazo: el identificador viaja en la ruta (tarea 4.1).</summary>
public sealed record RechazarReservaRequest(string Motivo);

