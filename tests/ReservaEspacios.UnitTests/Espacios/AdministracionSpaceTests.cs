using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.UnitTests.Espacios;

public sealed class AdministracionSpaceTests
{
    private readonly Mock<ISpaceRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task TC_ESP_002_Edicion_de_capacidad_no_afecta_reservas_previas()
    {
        var space = SpaceDePrueba(nombre: "Auditorio Central", capacidad: 200);
        var reservas = new FakeReservaStore(space.Id);
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new EditarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new EditarSpaceCommand(
            space.Id, "Auditorio Central", EspacioTipo.Auditorio, 180,
            "Bloque Central", new TimeOnly(8, 0), new TimeOnly(18, 0)));

        result.IsSuccess.Should().BeTrue();
        space.Capacidad.Should().Be(180);
        space.Nombre.Should().Be("Auditorio Central");
        reservas.Reservas.Should().ContainSingle(r => r.Estado == "APROBADA");
        _repository.Verify(x => x.Remove(It.IsAny<Space>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_ESP_002_Desactivacion_con_reserva_futura_la_conserva()
    {
        var space = SpaceDePrueba(nombre: "Sala de Cómputo 2");
        var reservas = new FakeReservaStore(space.Id);
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new DesactivarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new DesactivarSpaceCommand(space.Id));

        result.IsSuccess.Should().BeTrue();
        space.Activo.Should().BeFalse();
        reservas.Reservas.Should().ContainSingle(r =>
            r.SpaceId == space.Id && r.Estado == "APROBADA" && r.Inicio > DateTimeOffset.UtcNow);
        _repository.Verify(x => x.Remove(It.IsAny<Space>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_ESP_002_Reactivacion_devuelve_el_espacio_al_catalogo()
    {
        var space = SpaceDePrueba(nombre: "Sala de Cómputo 2", activo: false);
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new ReactivarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new ReactivarSpaceCommand(space.Id));

        result.IsSuccess.Should().BeTrue();
        space.Activo.Should().BeTrue();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_ESP_002_Edicion_con_nombre_ajeno_devuelve_NOMBRE_DUPLICADO()
    {
        var space = SpaceDePrueba();
        var otro = SpaceDePrueba(nombre: "Laboratorio B-202");
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        _repository.Setup(x => x.GetByNombreAsync(otro.Nombre, It.IsAny<CancellationToken>())).ReturnsAsync(otro);
        var sut = new EditarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new EditarSpaceCommand(
            space.Id, otro.Nombre, EspacioTipo.Laboratorio, 30,
            "Bloque A", new TimeOnly(8, 0), new TimeOnly(18, 0)));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.NombreDuplicado);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_ESP_002_Edicion_con_el_mismo_nombre_se_permite()
    {
        var space = SpaceDePrueba();
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        _repository.Setup(x => x.GetByNombreAsync(space.Nombre, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new EditarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new EditarSpaceCommand(
            space.Id, space.Nombre, EspacioTipo.Laboratorio, 35,
            "Bloque A", new TimeOnly(8, 0), new TimeOnly(18, 0)));

        result.IsSuccess.Should().BeTrue();
        space.Capacidad.Should().Be(35);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task TC_ESP_002_Edicion_con_capacidad_invalida_devuelve_error(int capacidad)
    {
        var space = SpaceDePrueba();
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new EditarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new EditarSpaceCommand(
            space.Id, space.Nombre, EspacioTipo.Laboratorio, capacidad,
            "Bloque A", new TimeOnly(8, 0), new TimeOnly(18, 0)));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.CapacidadInvalida);
    }

    [Fact]
    public async Task TC_ESP_002_Edicion_con_horario_invalido_devuelve_error()
    {
        var space = SpaceDePrueba();
        _repository.Setup(x => x.GetByIdAsync(space.Id, It.IsAny<CancellationToken>())).ReturnsAsync(space);
        var sut = new EditarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new EditarSpaceCommand(
            space.Id, space.Nombre, EspacioTipo.Laboratorio, 30,
            "Bloque A", new TimeOnly(18, 0), new TimeOnly(8, 0)));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.HorarioInvalido);
    }

    [Fact]
    public async Task TC_ESP_002_Espacio_inexistente_devuelve_error()
    {
        var id = Guid.NewGuid();
        var sut = new DesactivarSpace(_repository.Object, _unitOfWork.Object);

        var result = await sut.HandleAsync(new DesactivarSpaceCommand(id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DomainErrors.EspacioNoEncontrado);
    }

    private static Space SpaceDePrueba(
        string nombre = "Laboratorio A-101",
        int capacidad = 30,
        bool activo = true) => new(
            Guid.NewGuid(),
            nombre,
            EspacioTipo.Laboratorio,
            capacidad,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)),
            activo);

    /// <summary>
    /// Sustituto de la futura tabla `reservations` (llegará en el siguiente change).
    /// Permite comprobar que desactivar no elimina reservas futuras.
    /// </summary>
    private sealed record ReservaFuturaSimulada(
        Guid Id, Guid SpaceId, string Estado, DateTimeOffset Inicio, DateTimeOffset Fin);

    private sealed class FakeReservaStore(Guid spaceId)
    {
        public List<ReservaFuturaSimulada> Reservas { get; } =
        [
            new ReservaFuturaSimulada(
                Guid.NewGuid(),
                spaceId,
                "APROBADA",
                DateTimeOffset.UtcNow.AddDays(1).AddHours(10),
                DateTimeOffset.UtcNow.AddDays(1).AddHours(11))
        ];
    }
}
