using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Web.Components.Layout;
using ReservaEspacios.Web.Components.Pages;

namespace ReservaEspacios.E2ETests;

/// <summary>
/// AUD-002/REP-002 en la capa de presentación: la portada y el menú solo ofrecen
/// auditoría y reportes a coordinación/administración, y las vistas muestran las filas
/// devueltas por la Facade con los códigos estables de error.
/// </summary>
public sealed class AuditoriaYReportesUITests
{
    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public void TC_UI_005_Rol_privilegiado_ve_enlaces_de_auditoria_y_reporte(Rol rol)
    {
        using var ctx = CrearContexto(rol);

        var menu = ctx.Render<NavMenu>().Markup;
        menu.Should().Contain("/auditoria").And.Contain("/reportes/ocupacion");

        ctx.Dispose();
        using var portadaCtx = CrearContexto(rol);
        var portada = portadaCtx.Render<Home>().Markup;
        portada.Should().Contain("Consultar auditoría")
            .And.Contain("Reporte de ocupación");
    }

    [Fact]
    public void TC_UI_005_Solicitante_no_ve_enlaces_de_auditoria_ni_reporte()
    {
        using var ctx = CrearContexto(Rol.Solicitante);

        var menu = ctx.Render<NavMenu>().Markup;
        menu.Should().NotContain("/auditoria").And.NotContain("/reportes/ocupacion");

        ctx.Dispose();
        using var portadaCtx = CrearContexto(Rol.Solicitante);
        var portada = portadaCtx.Render<Home>().Markup;
        portada.Should().NotContain("Consultar auditoría")
            .And.NotContain("Reporte de ocupación");
    }

    [Fact]
    public void TC_UI_005_Anonimo_no_ve_enlaces_de_auditoria_ni_reporte()
    {
        using var ctx = CrearContexto(null);

        var menu = ctx.Render<NavMenu>().Markup;
        menu.Should().NotContain("/auditoria").And.NotContain("/reportes/ocupacion");

        ctx.Dispose();
        using var portadaCtx = CrearContexto(null);
        var portada = portadaCtx.Render<Home>().Markup;
        portada.Should().NotContain("Consultar auditoría")
            .And.NotContain("Reporte de ocupación");
    }

    [Fact]
    public void TC_AUD_002_La_vista_de_auditoria_muestra_actor_rol_y_detalle()
    {
        var fila = new AuditoriaDto(
            Guid.NewGuid(), "Reservation", Guid.NewGuid(), "APROBADA", Guid.NewGuid(),
            "Coordinador de espacios", "Coordinador de espacios", DateTimeOffset.UtcNow,
            "{\"motivo\":\"Espacio en mantención\"}");
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarAuditoria(
                It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<AuditoriaDto>>.Success([fila]));
        using var ctx = CrearContexto(Rol.Coordinador, facade.Object);

        var cut = ctx.Render<Auditoria>();

        var filas = cut.FindAll("[data-testid='fila-auditoria']");
        filas.Should().ContainSingle();
        filas[0].QuerySelector("[data-testid='accion']")!.TextContent.Trim()
            .Should().Be("APROBADA");
        filas[0].QuerySelector("[data-testid='actor']")!.TextContent.Trim()
            .Should().Be("Coordinador de espacios");
        cut.Markup.Should().Contain("Coordinador de espacios")
            .And.Contain("mantención");
    }

    [Fact]
    public void TC_AUD_002_Filtrar_envia_la_entidad_actor_y_el_rango_a_la_facade()
    {
        var actorId = Guid.NewGuid();
        FiltroAuditoria? capturado = null;
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarAuditoria(
                It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()))
            .Callback<FiltroAuditoria, CancellationToken>((filtro, _) => capturado = filtro)
            .ReturnsAsync(Result<IReadOnlyList<AuditoriaDto>>.Success([]));
        using var ctx = CrearContexto(Rol.Administrador, facade.Object);

        var cut = ctx.Render<Auditoria>();
        cut.Find("#entidad").Change("User");
        cut.Find("#actorId").Change(actorId.ToString());
        cut.Find("#desde").Change("2026-11-20T00:00:00Z");
        cut.Find("#hasta").Change("2026-11-21T00:00:00Z");
        cut.Find("#filtrar-auditoria").Click();

        capturado.Should().NotBeNull();
        capturado!.Entidad.Should().Be("User");
        capturado.ActorId.Should().Be(actorId);
        capturado.Desde.Should().NotBeNull();
        capturado.Hasta.Should().NotBeNull();
    }

    [Fact]
    public void TC_AUD_002_Solicitante_ve_NO_AUTORIZADO_y_no_llama_a_la_facade()
    {
        var facade = new Mock<IReservaEspaciosFacade>();
        using var ctx = CrearContexto(Rol.Solicitante, facade.Object);

        var cut = ctx.Render<Auditoria>();

        cut.Find("[data-testid='error']").TextContent.Trim()
            .Should().Be(DomainErrors.NoAutorizado);
        facade.Verify(x => x.ConsultarAuditoria(
            It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TC_REP_001_La_vista_muestra_las_metricas_de_ocupacion()
    {
        var espacioId = Guid.NewGuid();
        var fila = new DetalleOcupacionDto(
            espacioId, "Laboratorio A-101", 2, 1.5, 20, 7.5);
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.GenerarReporte(
                It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReporteOcupacionDto>.Success(
                new ReporteOcupacionDto([fila], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        using var ctx = CrearContexto(Rol.Coordinador, facade.Object);

        var cut = ctx.Render<ReporteOcupacion>();

        var filas = cut.FindAll("[data-testid='fila-reporte']");
        filas.Should().ContainSingle();
        filas[0].QuerySelector("[data-testid='reservas']")!.TextContent.Trim().Should().Be("2");
        filas[0].QuerySelector("[data-testid='horas-reservadas']")!.TextContent.Trim()
            .Should().Be(1.5.ToString());
        filas[0].QuerySelector("[data-testid='porcentaje']")!.TextContent.Trim()
            .Should().Be($"{7.5.ToString()} %");
    }

    [Fact]
    public void TC_REP_001_Generar_envia_el_espacio_y_el_rango_a_la_facade()
    {
        var espacioId = Guid.NewGuid();
        FiltroReporte? capturado = null;
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.GenerarReporte(
                It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()))
            .Callback<FiltroReporte, CancellationToken>((filtro, _) => capturado = filtro)
            .ReturnsAsync(Result<ReporteOcupacionDto>.Success(
                new ReporteOcupacionDto([], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        using var ctx = CrearContexto(Rol.Administrador, facade.Object);

        var cut = ctx.Render<ReporteOcupacion>();
        cut.Find("#spaceId").Change(espacioId.ToString());
        cut.Find("#desde").Change("2026-11-20");
        cut.Find("#hasta").Change("2026-11-21");
        cut.Find("#generar-reporte").Click();

        capturado.Should().NotBeNull();
        capturado!.SpaceId.Should().Be(espacioId);
        // `DateTimeOffset.TryParse` usa la zona local; se comprueba la fecha local.
        capturado.Desde.DateTime.Date.Should().Be(new DateTime(2026, 11, 20));
        capturado.Hasta.DateTime.Date.Should().Be(new DateTime(2026, 11, 21));
    }

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public void TC_REP_001_La_vista_muestra_el_codigo_de_error_de_la_facade(Rol rol)
    {
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.GenerarReporte(
                It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReporteOcupacionDto>.Failure(DomainErrors.RangoInvalido));
        using var ctx = CrearContexto(rol, facade.Object);

        var cut = ctx.Render<ReporteOcupacion>();

        cut.Find("[data-testid='error-consulta']").TextContent.Trim()
            .Should().Be(DomainErrors.RangoInvalido);
    }

    [Fact]
    public void TC_REP_002_Solicitante_ve_NO_AUTORIZADO_y_no_llama_a_la_facade()
    {
        var facade = new Mock<IReservaEspaciosFacade>();
        using var ctx = CrearContexto(Rol.Solicitante, facade.Object);

        var cut = ctx.Render<ReporteOcupacion>();

        cut.Find("[data-testid='error']").TextContent.Trim()
            .Should().Be(DomainErrors.NoAutorizado);
        facade.Verify(x => x.GenerarReporte(
            It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BunitContext CrearContexto(
        Rol? rol, IReservaEspaciosFacade? facade = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IEvaluadorPermisos>(new EvaluadorPermisos());
        ctx.Services.AddSingleton<IUsuarioActual>(new UsuarioDePrueba(rol));
        if (facade is not null)
        {
            ctx.Services.AddSingleton(facade);
        }
        return ctx;
    }

    private sealed record UsuarioDePrueba(Rol? RolSesion) : IUsuarioActual
    {
        public Rol Rol => RolSesion ?? Rol.Solicitante;
        public Guid? UsuarioId => RolSesion is null ? null : Guid.NewGuid();
    }
}
