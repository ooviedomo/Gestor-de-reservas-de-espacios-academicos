using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.UnitTests.Espacios;

public sealed class RegistrarSpaceTests
{
    private readonly Mock<ISpaceRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task TC_ESP_001_Registro_valido_guarda_el_espacio()
    {
        var command = ComandoValido();
        var sut = new RegistrarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repository.Verify(x => x.AddAsync(
            It.Is<Space>(s => s.Nombre == command.Nombre && s.Activo),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_ESP_001_Nombre_duplicado_devuelve_error()
    {
        var command = ComandoValido();
        _repository.Setup(x => x.GetByNombreAsync(command.Nombre, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Space(
                Guid.NewGuid(), command.Nombre, command.Tipo, command.Capacidad,
                command.Ubicacion, new HorarioAtencion(command.Apertura, command.Cierre)));
        var sut = new RegistrarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.NombreDuplicado);
        _repository.Verify(x => x.AddAsync(It.IsAny<Space>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task TC_ESP_001_Capacidad_invalida_devuelve_error(int capacidad)
    {
        var sut = new RegistrarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(ComandoValido() with { Capacidad = capacidad });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.CapacidadInvalida);
    }

    [Fact]
    public async Task TC_ESP_001_Horario_invalido_devuelve_error()
    {
        var sut = new RegistrarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(ComandoValido() with
        {
            Apertura = new TimeOnly(18, 0),
            Cierre = new TimeOnly(8, 0)
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.HorarioInvalido);
    }

    private static RegistrarSpaceCommand ComandoValido() => new(
        "Laboratorio A-101",
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new TimeOnly(8, 0),
        new TimeOnly(18, 0));
}
