using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Infrastructure.Reservas;

namespace ReservaEspacios.UnitTests.Disponibilidad;

/// <summary>
/// ADR-002 / tarea 3.1: el Decorator de escritura invalida la caché de disponibilidad
/// solo cuando la mutación se persistió correctamente.
/// </summary>
public sealed class InvalidarCacheReservaDecoratorTests
{
    private readonly Mock<IReservaRepository> _inner = new();
    private readonly Mock<IDisponibilidadCache> _cache = new();
    private readonly InvalidarCacheReservaDecorator _sut;

    public InvalidarCacheReservaDecoratorTests()
    {
        _sut = new InvalidarCacheReservaDecorator(_inner.Object, _cache.Object);
    }

    [Fact]
    public async Task TC_DISP_INV_001_Escritura_exitosa_invalida_la_cache_del_espacio()
    {
        var reserva = Nueva();

        await _sut.AddAsync(reserva);

        _inner.Verify(x => x.AddAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(x => x.Invalidar(reserva.SpaceId), Times.Once);
    }

    [Fact]
    public async Task TC_DISP_INV_002_Fallo_de_escritura_no_invalida_la_cache()
    {
        _inner.Setup(x => x.AddAsync(
                It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fallo de persistencia"));

        var act = () => _sut.AddAsync(Nueva());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _cache.Verify(x => x.Invalidar(It.IsAny<Guid>()), Times.Never);
    }

    private static Reservation Nueva() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero),
        "Clase de laboratorio",
        20);
}
