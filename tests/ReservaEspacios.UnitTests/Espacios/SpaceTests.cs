using FluentAssertions;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using Xunit;

namespace ReservaEspacios.UnitTests.Espacios;

public sealed class SpaceTests
{
    [Fact]
    public void TC_ESP_001_Registro_valido_queda_activo()
    {
        var space = new Space(
            Guid.NewGuid(),
            "Laboratorio A-101",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));

        space.Activo.Should().BeTrue();
        space.Nombre.Should().Be("Laboratorio A-101");
        space.Capacidad.Should().Be(30);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TC_ESP_001_Capacidad_invalida_se_rechaza(int capacidad)
    {
        var act = () => new Space(
            Guid.NewGuid(), "Laboratorio A-101", EspacioTipo.Laboratorio, capacidad, "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.CapacidadInvalida);
    }

    [Fact]
    public void TC_ESP_001_Horario_invalido_se_rechaza()
    {
        var act = () => new HorarioAtencion(new TimeOnly(18, 0), new TimeOnly(8, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.HorarioInvalido);
    }

    [Fact]
    public void TC_ESP_001_Horario_con_apertura_igual_al_cierre_se_rechaza()
    {
        var act = () => new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(8, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.HorarioInvalido);
    }
}
