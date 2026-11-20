using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Notificaciones;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// NOT-001/002/003 sobre el host real y PostgreSQL local (ADR-003). El worker no se
/// dispara por temporizador: se invoca el procesador con un proveedor SMTP falso.
/// </summary>
public sealed class NotificacionesIntegracionTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_NOT_001_Aprobacion_inserta_outbox_y_auditoria_en_la_misma_transaccion()
    {
        var coordinadorId = Guid.NewGuid();
        using var factory = CrearFactory(Rol.Coordinador, coordinadorId);
        var (solicitanteId, email) = await SembrarSolicitanteAsync(factory);
        var espacioId = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacioId, solicitanteId);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Aprobada);
        var notificacion = await contexto.Notificaciones.SingleAsync(n => n.ReservaId == reservaId);
        notificacion.Canal.Should().Be("CORREO");
        notificacion.Estado.Should().Be("PENDIENTE");
        notificacion.Destinatario.Should().Be(email);
        notificacion.Intentos.Should().Be(0);
        notificacion.Cuerpo.Should().Contain("Reserva aprobada");
        (await contexto.AuditLogs.SingleAsync(a =>
            a.EntidadId == reservaId && a.Accion == "APROBADA")).ActorId
            .Should().Be(coordinadorId);
    }

    [Fact]
    public async Task TC_NOT_001_Rechazo_y_cancelacion_incluyen_el_motivo()
    {
        var coordinadorId = Guid.NewGuid();
        using var factory = CrearFactory(Rol.Coordinador, coordinadorId);
        var (solicitanteId, _) = await SembrarSolicitanteAsync(factory);
        var espacioId = await SembrarEspacioAsync();
        var rechazadaId = await SembrarReservaPendienteAsync(espacioId, solicitanteId);
        var canceladaId = await SembrarReservaAprobadaAsync(espacioId, solicitanteId);
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync(
            $"/api/reservas/{rechazadaId}/rechazo",
            new { motivo = "Espacio en mantención" })).StatusCode.Should().Be(HttpStatusCode.OK);
        using (var solicitante = CrearFactory(Rol.Solicitante, solicitanteId))
        {
            (await solicitante.CreateClient().PostAsJsonAsync(
                $"/api/reservas/{canceladaId}/cancelacion",
                new { motivo = "Cambio de planes" })).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        await using var contexto = fixture.CreateContext();
        var notificaciones = await contexto.Notificaciones
            .Where(n => n.ReservaId == rechazadaId || n.ReservaId == canceladaId)
            .ToListAsync();
        notificaciones.Should().HaveCount(2);
        notificaciones.Single(n => n.ReservaId == rechazadaId).Cuerpo
            .Should().Contain("Espacio en mantención");
        notificaciones.Single(n => n.ReservaId == canceladaId).Cuerpo
            .Should().Contain("Cambio de planes");
    }

    [Fact]
    public async Task TC_NOT_001_Creacion_en_PENDIENTE_no_genera_notificacion()
    {
        Guid solicitanteId;
        using (var siembra = CrearFactory(Rol.Solicitante, Guid.NewGuid()))
        {
            (solicitanteId, _) = await SembrarSolicitanteAsync(siembra);
        }
        using var factory = CrearFactory(Rol.Solicitante, solicitanteId);
        var espacioId = await SembrarEspacioAsync();
        var proposito = $"Sin notificación {Guid.NewGuid():N}";
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/reservas", new
        {
            spaceId = espacioId,
            inicio = "2026-11-20T15:00:00Z",
            fin = "2026-11-20T16:00:00Z",
            proposito,
            asistentes = 10
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.Proposito == proposito);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        (await contexto.Notificaciones.CountAsync(n => n.ReservaId == reserva.Id)).Should().Be(0);
    }

    [Fact]
    public async Task TC_NOT_002_Envio_exitoso_marca_la_notificacion_como_ENVIADA()
    {
        var (reservaId, factory) = await AprobarConProveedorAsync(
            new EmailFalso(_ => Task.FromResult(Result.Success())));

        using (factory)
        {
            using var scope = factory.Services.CreateScope();
            var procesador = scope.ServiceProvider.GetRequiredService<IProcesadorNotificaciones>();
            (await procesador.ProcesarAsync()).Should().BeGreaterThan(0);
        }

        await using var contexto = fixture.CreateContext();
        var notificacion = await contexto.Notificaciones.SingleAsync(n => n.ReservaId == reservaId);
        notificacion.Estado.Should().Be("ENVIADA");
        notificacion.EnviadoEn.Should().NotBeNull();
        notificacion.UltimoError.Should().BeNull();
    }

    [Fact]
    public async Task TC_NOT_003_SMTP_caido_deja_la_reserva_APROBADA_y_el_outbox_PENDIENTE()
    {
        var (reservaId, factory) = await AprobarConProveedorAsync(
            new EmailFalso(_ => Task.FromResult(Result.Failure("SMTP_FALLO: conexión rechazada"))));

        using (factory)
        {
            using var scope = factory.Services.CreateScope();
            var procesador = scope.ServiceProvider.GetRequiredService<IProcesadorNotificaciones>();
            await procesador.ProcesarAsync();
        }

        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Aprobada);
        var notificacion = await contexto.Notificaciones.SingleAsync(n => n.ReservaId == reservaId);
        notificacion.Estado.Should().Be("PENDIENTE");
        notificacion.Intentos.Should().Be(1);
        notificacion.UltimoError.Should().Contain("SMTP_FALLO");
    }

    [Fact]
    public async Task TC_NOT_003_Si_falla_el_outbox_la_transicion_de_la_reserva_se_revierte()
    {
        using var factory = CrearFactory(
            Rol.Coordinador, Guid.NewGuid(),
            servicios => servicios.AddSingleton<INotificacionRepository>(
                new NotificacionRepositoryQueFalla()));
        var (solicitanteId, _) = await SembrarSolicitanteAsync(factory);
        var espacioId = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacioId, solicitanteId);

        using (var scope = factory.Services.CreateScope())
        {
            var aprobar = scope.ServiceProvider.GetRequiredService<ICommandHandler<AprobarReservaCommand>>();
            var actuar = () => aprobar.HandleAsync(new AprobarReservaCommand(reservaId));
            await actuar.Should().ThrowAsync<InvalidOperationException>();
        }

        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Pendiente);
        (await contexto.AuditLogs.CountAsync(a =>
            a.EntidadId == reservaId && a.Accion == "APROBADA")).Should().Be(0);
    }

    private async Task<(Guid ReservaId, WebApplicationFactory<Program> Factory)> AprobarConProveedorAsync(
        IEmailSender sender)
    {
        var factory = CrearFactory(
            Rol.Coordinador, Guid.NewGuid(),
            servicios => servicios.AddSingleton(sender));
        var (solicitanteId, _) = await SembrarSolicitanteAsync(factory);
        var espacioId = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacioId, solicitanteId);
        var respuesta = await factory.CreateClient().PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { });
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (reservaId, factory);
    }

    private WebApplicationFactory<Program> CrearFactory(
        Rol rol, Guid usuarioId, Action<IServiceCollection>? configurar = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.UseSetting("Notificaciones:IntervaloSegundos", "3600");
            builder.UseSetting("Reservas:CierreAutomatico:IntervaloSegundos", "3600");
            builder.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<IUsuarioActual>(new UsuarioAutenticado(rol, usuarioId));
                configurar?.Invoke(servicios);
            });
        });

    private async Task<(Guid Id, string Email)> SembrarSolicitanteAsync(
        WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var registrar = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = $"ana-{Guid.NewGuid():N}@universidad.edu";
        var resultado = await registrar.HandleAsync(
            new RegistrarSolicitanteCommand("Ana", email, "Segura123"));
        resultado.IsSuccess.Should().BeTrue();
        return (resultado.Value, email);
    }

    private async Task<Guid> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(), $"Laboratorio notificación {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio, 30, "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task<Guid> SembrarReservaPendienteAsync(Guid espacioId, Guid solicitanteId)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacioId, solicitanteId, Inicio, Fin, "Clase", 10);
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private async Task<Guid> SembrarReservaAprobadaAsync(Guid espacioId, Guid solicitanteId)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacioId, solicitanteId, Fin, Fin.AddHours(1), "Clase", 10);
        reserva.Aprobar(ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador));
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private sealed record UsuarioAutenticado(Rol Rol, Guid? UsuarioId) : IUsuarioActual;

    private sealed class EmailFalso(Func<EnviarCorreoCommand, Task<Result>> respuesta) : IEmailSender
    {
        public Task<Result> EnviarAsync(
            EnviarCorreoCommand command, CancellationToken cancellationToken = default) =>
            respuesta(command);
    }

    private sealed class NotificacionRepositoryQueFalla : INotificacionRepository
    {
        public Task AgregarAsync(
            NotificacionPendienteDto notificacion, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("OUTBOX_FALLO");

        public Task<IReadOnlyList<NotificacionPendienteDto>> ObtenerPendientesAsync(
            DateTimeOffset ahora, int limite, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task MarcarEnviadaAsync(
            Guid id, DateTimeOffset enviadoEn, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RegistrarFalloAsync(
            Guid id, int intentos, string error, DateTimeOffset proximoIntento, bool agotado,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
