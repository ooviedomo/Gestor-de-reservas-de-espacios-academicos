using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Web.Components.Pages;

namespace ReservaEspacios.E2ETests;

public sealed class AdministracionEspaciosTests
{
    [Theory]
    [InlineData("NOMBRE_DUPLICADO")]
    [InlineData("CAPACIDAD_INVALIDA")]
    [InlineData("HORARIO_INVALIDO")]
    public void TC_ESP_002_Formulario_muestra_el_codigo_de_error_devuelto(string codigo)
    {
        using var ctx = new BunitContext();
        var (registrar, desactivar, catalogo) = RegistrarServicios(ctx);
        registrar.Setup(x => x.HandleAsync(It.IsAny<RegistrarSpaceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(codigo));
        var cut = ctx.Render<AdministracionEspacios>();

        LlenarFormulario(cut);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']").TextContent.Trim().Should().Be(codigo));
    }

    [Fact]
    public void TC_ESP_002_Formulario_valida_horario_invalido_localmente()
    {
        using var ctx = new BunitContext();
        var (registrar, _, _) = RegistrarServicios(ctx);
        var cut = ctx.Render<AdministracionEspacios>();

        LlenarFormulario(cut);
        cut.Find("#apertura").Change("no-es-una-hora");
        cut.Find("#cierre").Change("08:00");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='error']").TextContent.Trim().Should().Be("HORARIO_INVALIDO"));
        registrar.Verify(
            x => x.HandleAsync(It.IsAny<RegistrarSpaceCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void TC_ESP_002_Desactivar_con_reservas_futuras_muestra_aviso_y_confirma()
    {
        using var ctx = new BunitContext();
        var (_, desactivar, catalogo) = RegistrarServicios(ctx);
        var id = Guid.NewGuid();
        catalogo.Setup(x => x.HandleAsync(It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
            [
                new SpaceCatalogoDto(
                    id, "Sala de Cómputo 2", EspacioTipo.SalaComputo, 30, "Bloque A", true)
            ]));
        desactivar.Setup(x => x.HandleAsync(It.IsAny<DesactivarSpaceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var cut = ctx.Render<AdministracionEspacios>();

        cut.Find($"[data-testid='desactivar-{id}']").Click();

        cut.Find("[data-testid='aviso-conservacion']")
            .TextContent.Should().Contain("reservas existentes se conservan");

        cut.Find("[data-testid='confirmar-desactivacion']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='exito']")
            .TextContent.Should().Contain("reservas existentes se conservan"));
        desactivar.Verify(
            x => x.HandleAsync(
                It.Is<DesactivarSpaceCommand>(c => c.Id == id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (
        Mock<IRegistrarSpace>,
        Mock<IDesactivarSpace>,
        Mock<IConsultarCatalogo>) RegistrarServicios(BunitContext ctx)
    {
        var registrar = new Mock<IRegistrarSpace>();
        var desactivar = new Mock<IDesactivarSpace>();
        var catalogo = new Mock<IConsultarCatalogo>();

        registrar.Setup(x => x.HandleAsync(It.IsAny<RegistrarSpaceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));
        catalogo.Setup(x => x.HandleAsync(It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
                Array.Empty<SpaceCatalogoDto>()));

        ctx.Services.AddSingleton(registrar.Object);
        ctx.Services.AddSingleton(desactivar.Object);
        ctx.Services.AddSingleton(catalogo.Object);
        return (registrar, desactivar, catalogo);
    }

    private static void LlenarFormulario(
        Bunit.IRenderedComponent<AdministracionEspacios> cut)
    {
        cut.Find("#nombre").Change("Laboratorio A-101");
        cut.Find("#capacidad").Change("30");
        cut.Find("#ubicacion").Change("Bloque A");
    }
}
