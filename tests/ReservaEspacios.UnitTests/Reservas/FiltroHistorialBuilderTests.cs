using FluentAssertions;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>ADR-002 / tarea 5.3: el Builder de filtros de RES-004 debe estar ejercitado por pruebas.</summary>
public sealed class FiltroHistorialBuilderTests
{
    [Fact]
    public void TC_RES_004_Builder_sin_filtros_devuelve_todo_nulo()
    {
        var filtro = new FiltroHistorialBuilder().Build();

        filtro.UsuarioId.Should().BeNull();
        filtro.Estado.Should().BeNull();
        filtro.Desde.Should().BeNull();
        filtro.Hasta.Should().BeNull();
        filtro.SpaceId.Should().BeNull();
    }

    [Fact]
    public void TC_RES_004_Builder_encadena_todos_los_filtros()
    {
        var usuarioId = Guid.NewGuid();
        var espacioId = Guid.NewGuid();
        var desde = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var hasta = new DateTimeOffset(2026, 11, 30, 0, 0, 0, TimeSpan.Zero);

        var filtro = new FiltroHistorialBuilder()
            .ConUsuario(usuarioId)
            .ConEstado(EstadoReserva.Cancelada)
            .Desde(desde)
            .Hasta(hasta)
            .ConSpace(espacioId)
            .Build();

        filtro.UsuarioId.Should().Be(usuarioId);
        filtro.Estado.Should().Be(EstadoReserva.Cancelada);
        filtro.Desde.Should().Be(desde);
        filtro.Hasta.Should().Be(hasta);
        filtro.SpaceId.Should().Be(espacioId);
    }
}
