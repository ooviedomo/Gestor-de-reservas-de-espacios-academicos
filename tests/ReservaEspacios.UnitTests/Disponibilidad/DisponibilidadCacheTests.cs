using FluentAssertions;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Infrastructure.Disponibilidad;

namespace ReservaEspacios.UnitTests.Disponibilidad;

/// <summary>Tarea 3.1: la invalidación afecta solo al espacio de la reserva mutada.</summary>
public sealed class DisponibilidadCacheTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TC_DISP_CACHE_001_Invalidar_borra_solo_las_franjas_del_espacio()
    {
        var espacio = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var sut = new DisponibilidadCache();
        var clave = new DisponibilidadCacheKey(espacio, Inicio, Inicio.AddHours(1));
        var otraClave = new DisponibilidadCacheKey(otro, Inicio, Inicio.AddHours(1));
        var dto = new DisponibilidadDto(EstadosDisponibilidad.Disponible, MotivosDisponibilidad.Ninguno);
        sut.Set(clave, dto);
        sut.Set(otraClave, dto);

        sut.Invalidar(espacio);

        sut.Get(clave).Should().BeNull();
        sut.Get(otraClave).Should().Be(dto);
    }
}
