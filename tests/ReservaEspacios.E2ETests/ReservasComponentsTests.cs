using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Web.Components.Pages;
using CancelarReservaPage = ReservaEspacios.Web.Components.Pages.CancelarReserva;

namespace ReservaEspacios.E2ETests;

/// <summary>
/// Tarea 3.5: los formularios de alta y cancelación muestran los códigos estables
/// devueltos por los comandos, o los validan localmente antes de invocarlos.
/// </summary>
public sealed class ReservasComponentsTests
{
    [Theory]
    [InlineData("RANGO_INVALIDO")]
    [InlineData("CAPACIDAD_EXCEDIDA")]
    [InlineData("NO_AUTORIZADO")]
    public void TC_RES_001_Formulario_alta_muestra_el_codigo_devuelto_por_el_comando(string codigo)
    {
        using var ctx = new BunitContext();
        var crear = new Mock<ICommandHandler<CrearReservaCommand, Guid>>();
        crear.Setup(x => x.HandleAsync(
                It.IsAny<CrearReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(codigo));
        RegistrarServiciosDeAlta(ctx, crear.Object);

        var cut = ctx.Render<SolicitarReserva>();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(codigo));
    }

    [Fact]
    public void TC_RES_005_Formulario_alta_muestra_DURACION_MAXIMA_EXCEDIDA()
    {
        using var ctx = new BunitContext();
        var crear = new Mock<ICommandHandler<CrearReservaCommand, Guid>>();
        crear.Setup(x => x.HandleAsync(
                It.IsAny<CrearReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(DomainErrors.DuracionMaximaExcedida));
        RegistrarServiciosDeAlta(ctx, crear.Object);

        var cut = ctx.Render<SolicitarReserva>();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(DomainErrors.DuracionMaximaExcedida));
    }

    [Fact]
    public void TC_RES_001_Formulario_alta_valida_rango_invalido_localmente()
    {
        using var ctx = new BunitContext();
        var crear = new Mock<ICommandHandler<CrearReservaCommand, Guid>>();
        RegistrarServiciosDeAlta(ctx, crear.Object);

        var cut = ctx.Render<SolicitarReserva>();
        cut.Find("#inicio").Change("11:00");
        cut.Find("#fin").Change("10:00");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(DomainErrors.RangoInvalido));
        crear.Verify(x => x.HandleAsync(
            It.IsAny<CrearReservaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TC_UI_005_Formulario_alta_anuncia_el_formato_HH_mm()
    {
        using var ctx = new BunitContext();
        var crear = new Mock<ICommandHandler<CrearReservaCommand, Guid>>();
        RegistrarServiciosDeAlta(ctx, crear.Object);

        var cut = ctx.Render<SolicitarReserva>();

        cut.Find("#inicio").GetAttribute("type").Should().Be("time");
        cut.Find("#inicio").GetAttribute("title").Should().Be("HH:mm");
        cut.Find("#fin").GetAttribute("type").Should().Be("time");
        cut.Find("#fin").GetAttribute("title").Should().Be("HH:mm");
        cut.Find("#fecha").GetAttribute("title").Should().Be("dd/MM/yyyy");
    }

    [Theory]
    [InlineData("MOTIVO_OBLIGATORIO")]
    [InlineData("NO_AUTORIZADO")]
    [InlineData("TRANSICION_INVALIDA")]
    public void TC_RES_003_Formulario_cancelacion_muestra_el_codigo_devuelto(string codigo)
    {
        using var ctx = new BunitContext();
        var cancelar = new Mock<ICommandHandler<CancelarReservaCommand>>();
        cancelar.Setup(x => x.HandleAsync(
                It.IsAny<CancelarReservaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(codigo));
        ctx.Services.AddSingleton(cancelar.Object);

        var cut = ctx.Render<CancelarReservaPage>();
        cut.Find("#reservaId").Change(Guid.NewGuid().ToString());
        cut.Find("#motivo").Change("Docencia reprogramada");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(codigo));
    }

    [Fact]
    public void TC_RES_003_Formulario_cancelacion_valida_motivo_localmente()
    {
        using var ctx = new BunitContext();
        var cancelar = new Mock<ICommandHandler<CancelarReservaCommand>>();
        ctx.Services.AddSingleton(cancelar.Object);

        var cut = ctx.Render<CancelarReservaPage>();
        cut.Find("#reservaId").Change(Guid.NewGuid().ToString());
        cut.Find("#motivo").Change("   ");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(DomainErrors.MotivoObligatorio));
        cancelar.Verify(x => x.HandleAsync(
            It.IsAny<CancelarReservaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void RegistrarServiciosDeAlta(
        BunitContext ctx, ICommandHandler<CrearReservaCommand, Guid> crear)
    {
        var id = Guid.NewGuid();
        var facade = new Mock<IReservaEspaciosFacade>();
        facade.Setup(x => x.ConsultarCatalogo(
                It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
            [
                new SpaceCatalogoDto(id, "Laboratorio A-101", EspacioTipo.Laboratorio, 30, "Bloque A", true)
            ]));

        ctx.Services.AddSingleton(crear);
        ctx.Services.AddSingleton(facade.Object);
    }
}
