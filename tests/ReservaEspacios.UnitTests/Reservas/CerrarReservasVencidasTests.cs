using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// COM-001 (tarea 4.2): el cierre automático consulta solo APROBADA vencidas, aplica
/// ActorTransicion.Sistema y persiste cada transición sin intervención de usuario.
/// </summary>
public sealed class CerrarReservasVencidasTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 11, 20, 17, 0, 0, TimeSpan.Zero);
    private readonly Guid _aprobadorId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();

    [Fact]
    public async Task TC_COM_001_Cierra_todas_las_aprobadas_vencidas_y_devuelve_el_conteo()
    {
        var primera = Aprobada(new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero));
        var segunda = Aprobada(new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero));
        _repository.Setup(x => x.ObtenerAprobadasVencidasAsync(
                Ahora, It.IsAny<CancellationToken>()))
            .ReturnsAsync([primera, segunda]);
        var sut = new CerrarReservasVencidas(_repository.Object);

        var completadas = await sut.EjecutarAsync(Ahora);

        completadas.Should().Be(2);
        primera.Estado.Should().Be(EstadoReserva.Completada);
        segunda.Estado.Should().Be(EstadoReserva.Completada);
        primera.CompletadoEn.Should().Be(Ahora);
        segunda.CompletadoEn.Should().Be(Ahora);
        // El cierre automático no sobrescribe al aprobador.
        primera.AprobadoPor.Should().Be(_aprobadorId);
        primera.ActorId.Should().Be(_aprobadorId);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TC_COM_001_Reserva_no_vencida_no_se_cierra()
    {
        var reserva = Aprobada(Ahora.AddHours(1));
        _repository.Setup(x => x.ObtenerAprobadasVencidasAsync(
                Ahora, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reserva]);
        var sut = new CerrarReservasVencidas(_repository.Object);

        var completadas = await sut.EjecutarAsync(Ahora);

        completadas.Should().Be(0);
        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
        reserva.CompletadoEn.Should().BeNull();
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_COM_001_Cierre_automatico_es_idempotente()
    {
        var reserva = Aprobada(Ahora.AddHours(-1));
        reserva.CompletarAutomaticamente(Ahora.AddMinutes(-1));
        _repository.Setup(x => x.ObtenerAprobadasVencidasAsync(
                Ahora, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reserva]);
        var sut = new CerrarReservasVencidas(_repository.Object);

        var completadas = await sut.EjecutarAsync(Ahora);

        completadas.Should().Be(0);
        reserva.CompletadoEn.Should().Be(Ahora.AddMinutes(-1));
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_COM_001_Sin_hora_explicita_usa_UTC()
    {
        _repository.Setup(x => x.ObtenerAprobadasVencidasAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Reservation>());
        var sut = new CerrarReservasVencidas(_repository.Object);
        var antes = DateTimeOffset.UtcNow;

        await sut.EjecutarAsync();

        _repository.Verify(x => x.ObtenerAprobadasVencidasAsync(
            It.Is<DateTimeOffset>(hora => hora >= antes && hora <= DateTimeOffset.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private Reservation Aprobada(DateTimeOffset fin)
    {
        var reserva = new Reservation(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            fin.AddHours(-1), fin, "Clase", 20);
        reserva.Aprobar(ActorTransicion.Usuario(_aprobadorId, Rol.Coordinador));
        return reserva;
    }
}
