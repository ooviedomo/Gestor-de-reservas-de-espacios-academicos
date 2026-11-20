using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Web.Components.Pages;

namespace ReservaEspacios.E2ETests;

/// <summary>
/// Tarea 4.4 (APR-001/RES-004): los componentes de aprobación e historial publican los
/// comandos ya verificados en Application y muestran los códigos estables y los datos
/// del historial sin duplicar reglas de dominio.
/// </summary>
public sealed class AprobacionesHistorialComponentTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreadoEn =
        new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TC_APR_001_Panel_muestra_pendientes_y_aprueba()
    {
        var reservaId = Guid.NewGuid();
        var historial = HistorialCon(Fila(reservaId, "PENDIENTE"));
        var aprobar = new Mock<ICommandHandler<AprobarReservaCommand>>();
        aprobar.Setup(x => x.HandleAsync(
                It.IsAny<AprobarReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        using var ctx = Contexto(Rol.Coordinador, historial.Object, aprobar.Object);

        var cut = ctx.Render<AprobacionesReserva>();

        cut.FindAll("[data-testid='fila']").Should().ContainSingle();
        cut.Find("[data-testid='fila']").TextContent.Should().Contain("Ana Pérez");
        cut.Find($"#aprobar-{reservaId}").Click();

        aprobar.Verify(x => x.HandleAsync(
            It.Is<AprobarReservaCommand>(c => c.ReservaId == reservaId),
            It.IsAny<CancellationToken>()), Times.Once);
        cut.WaitForAssertion(() => cut.Find("[data-testid='accion-exito']")
            .TextContent.Trim().Should().Be("Reserva aprobada."));
        // La aprobación recarga el panel de pendientes.
        historial.Verify(x => x.HandleAsync(
            It.IsAny<ConsultarHistorialCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void TC_APR_001_Panel_rechaza_con_el_motivo_escrito()
    {
        var reservaId = Guid.NewGuid();
        var historial = HistorialCon(Fila(reservaId, "PENDIENTE"));
        var rechazar = new Mock<ICommandHandler<RechazarReservaCommand>>();
        rechazar.Setup(x => x.HandleAsync(
                It.IsAny<RechazarReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        using var ctx = Contexto(Rol.Coordinador, historial.Object, rechazar: rechazar.Object);

        var cut = ctx.Render<AprobacionesReserva>();
        cut.Find($"#motivo-{reservaId}").Change("Espacio en mantención");
        cut.Find($"#rechazar-{reservaId}").Click();

        rechazar.Verify(x => x.HandleAsync(
            It.Is<RechazarReservaCommand>(c =>
                c.ReservaId == reservaId && c.Motivo == "Espacio en mantención"),
            It.IsAny<CancellationToken>()), Times.Once);
        cut.WaitForAssertion(() => cut.Find("[data-testid='accion-exito']")
            .TextContent.Trim().Should().Be("Reserva rechazada."));
    }

    [Fact]
    public void TC_APR_001_Panel_solicitante_ve_NO_AUTORIZADO_y_no_consulta()
    {
        var historial = HistorialCon([Fila(Guid.NewGuid(), "PENDIENTE")]);
        using var ctx = Contexto(Rol.Solicitante, historial.Object);

        var cut = ctx.Render<AprobacionesReserva>();

        cut.Find("[data-testid='error']").TextContent.Trim().Should().Be(DomainErrors.NoAutorizado);
        historial.Verify(x => x.HandleAsync(
            It.IsAny<ConsultarHistorialCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TC_APR_001_Panel_muestra_el_codigo_devuelto_por_el_comando()
    {
        var reservaId = Guid.NewGuid();
        var aprobar = new Mock<ICommandHandler<AprobarReservaCommand>>();
        aprobar.Setup(x => x.HandleAsync(
                It.IsAny<AprobarReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(DomainErrors.TransicionInvalida));
        using var ctx = Contexto(
            Rol.Coordinador, HistorialCon(Fila(reservaId, "PENDIENTE")).Object, aprobar.Object);

        var cut = ctx.Render<AprobacionesReserva>();
        cut.Find($"#aprobar-{reservaId}").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='accion-error']")
            .TextContent.Trim().Should().Be(DomainErrors.TransicionInvalida));
    }

    [Fact]
    public void TC_RES_004_Vista_muestra_actor_rol_y_motivo_de_rechazo()
    {
        var coordinadorId = Guid.NewGuid();
        var fila = Fila(Guid.NewGuid(), "RECHAZADA", coordinadorId);
        using var ctx = Contexto(Rol.Coordinador, HistorialCon(fila).Object);

        var cut = ctx.Render<HistorialReservas>();

        cut.FindAll("[data-testid='fila']").Should().ContainSingle();
        cut.Find("[data-testid='estado-fila']").TextContent.Trim().Should().Be("RECHAZADA");
        cut.Find("[data-testid='actor']").TextContent.Trim().Should().Be("Laura Gómez");
        cut.Find("[data-testid='rol-actor']").TextContent.Trim()
            .Should().Be("Coordinador de espacios");
        cut.Find("[data-testid='motivo-rechazo']").TextContent.Trim()
            .Should().Be("Espacio en mantención");
        cut.Find("[data-testid='fila']").TextContent.Should().Contain("Laboratorio A-101");
    }

    [Fact]
    public void TC_RES_004_Vista_envia_los_filtros_al_consultar()
    {
        var espacioId = Guid.NewGuid();
        var historial = HistorialCon([]);
        using var ctx = Contexto(Rol.Coordinador, historial.Object);

        var cut = ctx.Render<HistorialReservas>();
        cut.Find("#estado").Change("CANCELADA");
        cut.Find("#desde").Change("2026-11-01T00:00");
        cut.Find("#hasta").Change("2026-11-30T23:59");
        cut.Find("#spaceId").Change(espacioId.ToString());
        cut.Find("#filtrar").Click();

        historial.Verify(x => x.HandleAsync(
            It.Is<ConsultarHistorialCommand>(c =>
                c.Filtro.Estado == EstadoReserva.Cancelada &&
                c.Filtro.Desde != null &&
                c.Filtro.Hasta != null &&
                c.Filtro.SpaceId == espacioId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BunitContext Contexto(
        Rol rol,
        ICommandHandler<ConsultarHistorialCommand, IReadOnlyList<HistorialReservaDto>> historial,
        ICommandHandler<AprobarReservaCommand>? aprobar = null,
        ICommandHandler<RechazarReservaCommand>? rechazar = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(historial);
        ctx.Services.AddSingleton(aprobar ?? new Mock<ICommandHandler<AprobarReservaCommand>>().Object);
        ctx.Services.AddSingleton(rechazar ?? new Mock<ICommandHandler<RechazarReservaCommand>>().Object);
        ctx.Services.AddSingleton<IEvaluadorPermisos>(new EvaluadorPermisos());
        ctx.Services.AddSingleton<IUsuarioActual>(
            new UsuarioAutenticado(rol, Guid.NewGuid()));
        return ctx;
    }

    private static Mock<ICommandHandler<ConsultarHistorialCommand, IReadOnlyList<HistorialReservaDto>>>
        HistorialCon(params HistorialReservaDto[] filas)
    {
        var mock = new Mock<
            ICommandHandler<ConsultarHistorialCommand, IReadOnlyList<HistorialReservaDto>>>();
        mock.Setup(x => x.HandleAsync(
                It.IsAny<ConsultarHistorialCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<HistorialReservaDto>>.Success(filas));
        return mock;
    }

    private static HistorialReservaDto Fila(
        Guid id,
        string estado,
        Guid? actorId = null) => new(
            id, Guid.NewGuid(), "Laboratorio A-101", Inicio, Inicio.AddHours(1), estado,
            "Ana Pérez", actorId,
            actorId is null ? null : "Laura Gómez",
            actorId is null ? null : "Coordinador de espacios",
            estado == "RECHAZADA" ? "Espacio en mantención" : null,
            null, null, null, null, CreadoEn);

    private sealed class UsuarioAutenticado(Rol rol, Guid? id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
