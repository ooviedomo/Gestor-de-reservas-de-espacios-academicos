using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Infrastructure.Disponibilidad;

namespace ReservaEspacios.UnitTests.Disponibilidad;

/// <summary>ADR-002 / tarea 4.3: el Decorator debe estar ejercitado por una prueba.</summary>
public sealed class DisponibilidadCacheDecoratorTests
{
    private readonly Mock<IConsultarDisponibilidad> _inner = new();
    private readonly DisponibilidadCacheDecorator _sut;

    public DisponibilidadCacheDecoratorTests()
    {
        _inner.Setup(x => x.HandleAsync(
                It.IsAny<ConsultarDisponibilidadCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DisponibilidadDto>.Success(
                new DisponibilidadDto(
                    EstadosDisponibilidad.Disponible,
                    MotivosDisponibilidad.Ninguno)));
        _sut = new DisponibilidadCacheDecorator(_inner.Object, new DisponibilidadCache());
    }

    [Fact]
    public async Task TC_DISP_DEC_001_Dos_consultas_identicas_llaman_al_inner_una_sola_vez()
    {
        var command = Comando();

        var primero = await _sut.HandleAsync(command);
        var segundo = await _sut.HandleAsync(command);

        segundo.Value.Should().BeEquivalentTo(primero.Value);
        _inner.Verify(
            x => x.HandleAsync(It.IsAny<ConsultarDisponibilidadCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TC_DISP_DEC_002_Franja_distinta_no_usa_la_cache()
    {
        await _sut.HandleAsync(Comando());
        await _sut.HandleAsync(Comando() with { Fin = new DateTimeOffset(2026, 11, 20, 16, 30, 0, TimeSpan.Zero) });

        _inner.Verify(
            x => x.HandleAsync(It.IsAny<ConsultarDisponibilidadCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static ConsultarDisponibilidadCommand Comando() => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero));
}
