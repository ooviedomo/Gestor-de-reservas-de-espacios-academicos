using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;

namespace ReservaEspacios.UnitTests.Facade;

/// <summary>ADR-002 / tarea 4.3: el Facade debe estar ejercitado por una prueba.</summary>
public sealed class ReservaEspaciosFacadeTests
{
    private readonly Mock<IConsultarCatalogo> _catalogo = new();
    private readonly Mock<IConsultarDisponibilidad> _disponibilidad = new();
    private readonly ReservaEspaciosFacade _sut;

    public ReservaEspaciosFacadeTests()
    {
        _catalogo.Setup(x => x.HandleAsync(It.IsAny<FiltroCatalogo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<SpaceCatalogoDto>>.Success(
                Array.Empty<SpaceCatalogoDto>()));
        _disponibilidad.Setup(x => x.HandleAsync(
                It.IsAny<ConsultarDisponibilidadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DisponibilidadDto>.Success(
                new DisponibilidadDto("DISPONIBLE", "NINGUNO")));
        _sut = new ReservaEspaciosFacade(_catalogo.Object, _disponibilidad.Object);
    }

    [Fact]
    public async Task TC_FACADE_001_ConsultarCatalogo_delega_en_el_caso_de_uso()
    {
        var filtro = new FiltroCatalogoBuilder().Build();

        var resultado = await _sut.ConsultarCatalogo(filtro);

        resultado.IsSuccess.Should().BeTrue();
        _catalogo.Verify(
            x => x.HandleAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_FACADE_002_ConsultarDisponibilidad_delega_en_el_caso_de_uso()
    {
        var command = new ConsultarDisponibilidadCommand(
            Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

        var resultado = await _sut.ConsultarDisponibilidad(command);

        resultado.IsSuccess.Should().BeTrue();
        _disponibilidad.Verify(
            x => x.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
