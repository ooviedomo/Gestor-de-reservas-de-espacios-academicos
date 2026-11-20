using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Web.Security;

namespace ReservaEspacios.Web;

/// <summary>
/// Endpoints HTTP del change. Las reglas de autorización viven en
/// <see cref="IEvaluadorPermisos"/> y en los guards de Application, no aquí. Incluye
/// la consulta de auditoría (AUD-002) y el reporte de ocupación (REP-001/002).
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

        // UI-002: la UI envía un formulario normal. La cookie de Identity se escribe en
        // esta petición HTTP, antes de la redirección; el endpoint JSON anterior se
        // conserva para clientes que no son el navegador.
        app.MapPost("/acceso/autenticar", async (HttpContext contexto, AutenticarUsuario autenticar) =>
        {
            var formulario = await contexto.Request.ReadFormAsync();
            var resultado = await autenticar.HandleAsync(new AutenticarUsuarioCommand(
                formulario["email"].ToString(),
                formulario["contrasena"].ToString()));

            if (resultado.IsSuccess)
            {
                return Results.Redirect("/");
            }

            var codigo = resultado.ErrorCode ?? DomainErrors.CredencialesInvalidas;
            return Results.Redirect($"/acceso/login?error={Uri.EscapeDataString(codigo)}");
        });

        app.MapPost("/api/autenticacion/logout", async (ISesionWeb sesion) =>
        {
            await sesion.CerrarSesionAsync();
            return Results.Ok(new { cerrada = true });
        });

        // UI-004: el menú envía un formulario normal. La cookie se elimina en esta
        // petición HTTP y la respuesta devuelve al navegador a la portada. El endpoint
        // JSON anterior se conserva para clientes que no son la UI.
        app.MapPost("/acceso/cerrar-sesion", async (ISesionWeb sesion) =>
        {
            await sesion.CerrarSesionAsync();
            return Results.Redirect("/");
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

        app.MapGet("/api/reservas/historial", async (
            string? estado,
            DateTimeOffset? desde,
            DateTimeOffset? hasta,
            Guid? spaceId,
            ConsultarHistorial consultar,
            CancellationToken cancellationToken) =>
        {
            var estadoParseado = EstadoReservaExtensions.DesdeCodigo(estado);
            if (estado is not null && estadoParseado is null)
            {
                return Results.BadRequest(new { error = "ESTADO_INVALIDO" });
            }

            var filtro = new FiltroHistorialBuilder()
                .ConEstado(estadoParseado)
                .Desde(desde)
                .Hasta(hasta)
                .ConSpace(spaceId)
                .Build();
            var resultado = await consultar.HandleAsync(
                new ConsultarHistorialCommand(filtro), cancellationToken);

            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Error(resultado.ErrorCode);
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

        app.MapPost("/api/reservas/{reservaId:guid}/completacion", async (
            Guid reservaId,
            CompletarReserva completar) =>
        {
            var resultado = await completar.HandleAsync(new CompletarReservaCommand(reservaId));
            return resultado.IsSuccess
                ? Results.Ok(new { id = reservaId })
                : Error(resultado.ErrorCode);
        });

        app.MapGet("/api/auditoria", async (
            string? entidad,
            Guid? actorId,
            DateTimeOffset? desde,
            DateTimeOffset? hasta,
            ConsultarAuditoria consultar,
            CancellationToken cancellationToken) =>
        {
            var filtro = new FiltroAuditoria(entidad, actorId, desde, hasta);
            var resultado = await consultar.HandleAsync(filtro, cancellationToken);
            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Error(resultado.ErrorCode);
        });

        app.MapGet("/api/reportes/ocupacion", async (
            Guid? spaceId,
            DateTimeOffset? desde,
            DateTimeOffset? hasta,
            ConsultarReporteOcupacion consultar,
            CancellationToken cancellationToken) =>
        {
            var filtro = new ReporteBuilder()
                .Desde(desde ?? DateTimeOffset.UtcNow.AddMonths(-1))
                .Hasta(hasta ?? DateTimeOffset.UtcNow)
                .ParaEspacio(spaceId)
                .Build();
            var resultado = await consultar.HandleAsync(filtro, cancellationToken);
            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Error(resultado.ErrorCode);
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

}

/// <summary>Cuerpo de la cancelación: el identificador viaja en la ruta (tarea 3.4).</summary>
public sealed record CancelarReservaRequest(string Motivo);

/// <summary>Cuerpo del rechazo: el identificador viaja en la ruta (tarea 4.1).</summary>
public sealed record RechazarReservaRequest(string Motivo);

