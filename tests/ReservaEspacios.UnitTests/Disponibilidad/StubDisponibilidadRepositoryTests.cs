using FluentAssertions;
using ReservaEspacios.Infrastructure.Disponibilidad;

namespace ReservaEspacios.UnitTests.Disponibilidad;

/// <summary>
/// TC-DISP-AISL-001: el stub mantiene aislado el caso de uso de la persistencia
/// de reservas, que se incorporará en el siguiente change (tasks.md 3.1).
/// </summary>
public sealed class StubDisponibilidadRepositoryTests
{
    [Fact]
    public async Task TC_DISP_AISL_001_Stub_no_devuelve_reservas_y_no_toca_base_de_datos()
    {
        var sut = new StubDisponibilidadRepository();
        var inicio = new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

        var reservas = await sut.ObtenerReservasAsync(Guid.NewGuid(), inicio, inicio.AddHours(1));

        reservas.Should().BeEmpty();
    }
}
