using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Web.Components.Pages;

namespace ReservaEspacios.E2ETests;

public sealed class ConsultarDisponibilidadPageTests
{
    [Fact]
    public void TC_DISP_001_Componente_muestra_estado_y_motivo_devueltos_por_la_fachada()
    {
        using var ctx = new BunitContext();
        var id = Guid.NewGuid();
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarCatalogo(
                It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
            [
                new SpaceCatalogoDto(id, "Laboratorio A-101", EspacioTipo.Laboratorio, 30, "Bloque A", true)
            ]));
        facade.Setup(x => x.ConsultarDisponibilidad(
                It.IsAny<ConsultarDisponibilidadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DisponibilidadDto>.Success(
                new DisponibilidadDto(
                    EstadosDisponibilidad.NoDisponible,
                    MotivosDisponibilidad.SolapeConReserva)));
        ctx.Services.AddSingleton(facade.Object);

        var cut = ctx.Render<ConsultarDisponibilidadPage>();
        cut.Find("#consultar").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='estado']")
            .TextContent.Trim().Should().Be(EstadosDisponibilidad.NoDisponible));
        cut.Find("[data-testid='motivo']").TextContent.Trim()
            .Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    [Fact]
    public void TC_UI_005_Formulario_disponibilidad_anuncia_el_formato_HH_mm()
    {
        using var ctx = new BunitContext();
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarCatalogo(
                It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success([]));
        ctx.Services.AddSingleton(facade.Object);

        var cut = ctx.Render<ConsultarDisponibilidadPage>();

        cut.Find("#inicio").GetAttribute("type").Should().Be("time");
        cut.Find("#inicio").GetAttribute("title").Should().Be("HH:mm");
        cut.Find("#fin").GetAttribute("type").Should().Be("time");
        cut.Find("#fin").GetAttribute("title").Should().Be("HH:mm");
        cut.Find("#fecha").GetAttribute("title").Should().Be("dd/MM/yyyy");
    }

    [Fact]
    public void TC_DISP_001_Componente_muestra_mensaje_si_el_catalogo_falla()
    {
        using var ctx = new BunitContext();
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarCatalogo(
                It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("42P01: relation \"spaces\" does not exist"));
        ctx.Services.AddSingleton(facade.Object);

        var cut = ctx.Render<ConsultarDisponibilidadPage>();

        var mensaje = cut.Find("[data-testid='error']").TextContent.Trim();
        mensaje.Should().Contain("No se pudo cargar el catálogo de espacios");
        mensaje.Should().NotContain("42P01");
        mensaje.Should().NotContain("spaces");
    }
}
