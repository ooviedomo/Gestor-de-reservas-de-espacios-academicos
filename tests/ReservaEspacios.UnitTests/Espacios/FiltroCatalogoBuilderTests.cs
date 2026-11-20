using FluentAssertions;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.UnitTests.Espacios;

/// <summary>ADR-002 / tarea 4.3: el patrón Builder debe estar ejercitado por una prueba.</summary>
public sealed class FiltroCatalogoBuilderTests
{
    [Fact]
    public void TC_ESP_003_Builder_por_defecto_solo_activos()
    {
        var filtro = new FiltroCatalogoBuilder().Build();

        filtro.Estado.Should().Be(EstadoSpaceFiltro.Activo);
        filtro.Tipo.Should().BeNull();
        filtro.CapacidadMinima.Should().BeNull();
    }

    [Fact]
    public void TC_ESP_003_Builder_encadena_todos_los_filtros()
    {
        var filtro = new FiltroCatalogoBuilder()
            .ConTipo(EspacioTipo.SalaComputo)
            .ConCapacidadMinima(30)
            .ConEstado(EstadoSpaceFiltro.Inactivo)
            .Build();

        filtro.Tipo.Should().Be(EspacioTipo.SalaComputo);
        filtro.CapacidadMinima.Should().Be(30);
        filtro.Estado.Should().Be(EstadoSpaceFiltro.Inactivo);
    }
}
