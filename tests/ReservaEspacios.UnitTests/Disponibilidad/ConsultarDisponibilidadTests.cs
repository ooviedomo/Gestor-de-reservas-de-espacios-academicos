using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.UnitTests.Disponibilidad;

public sealed class ConsultarDisponibilidadTests
{
    private readonly Mock<ISpaceRepository> _spaces = new();
    private readonly Mock<IDisponibilidadRepository> _reservas = new();
    [Fact]
    public async Task TC_DISP_001_Espacio_libre()
    {
        var sut = CrearSut();

        var result = await sut.HandleAsync(Comando(10, 0, 11, 0));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
        result.Value.Motivo.Should().Be(MotivosDisponibilidad.Ninguno);
    }

    [Fact]
    public async Task TC_DISP_001_Espacio_inactivo()
    {
        var sut = CrearSut(activo: false);

        var result = await sut.HandleAsync(Comando(10, 0, 11, 0));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        result.Value.Motivo.Should().Be(MotivosDisponibilidad.EspacioInactivo);
        _reservas.Verify(
            x => x.ObtenerReservasAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_DISP_001_Fuera_del_horario_de_atencion()
    {
        var sut = CrearSut();

        var result = await sut.HandleAsync(Comando(19, 0, 20, 0));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        result.Value.Motivo.Should().Be(MotivosDisponibilidad.FueraDeHorario);
    }

    [Theory]
    [InlineData("PENDIENTE")]
    [InlineData("APROBADA")]
    public async Task TC_DISP_002_Reserva_bloqueante_solapa(string estado)
    {
        var sut = CrearSut();
        Reservas(Reserva(10, 0, 11, 0, estado));

        var result = await sut.HandleAsync(Comando(10, 30, 11, 30));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        result.Value.Motivo.Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    [Theory]
    [InlineData("CANCELADA")]
    [InlineData("RECHAZADA")]
    [InlineData("COMPLETADA")]
    public async Task TC_DISP_002_Reserva_no_bloqueante_no_impide_la_franja(string estado)
    {
        var sut = CrearSut();
        Reservas(Reserva(10, 0, 11, 0, estado));

        var result = await sut.HandleAsync(Comando(10, 0, 11, 0));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
    }

    [Fact]
    public async Task TC_DISP_002_Reservas_contiguas_no_se_solapan()
    {
        var sut = CrearSut();
        Reservas(Reserva(10, 0, 11, 0, "APROBADA"));

        var result = await sut.HandleAsync(Comando(11, 0, 12, 0));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
    }

    [Fact]
    public async Task TC_DISP_002_Solape_parcial_por_el_inicio()
    {
        var sut = CrearSut();
        Reservas(Reserva(10, 0, 11, 0, "APROBADA"));

        var result = await sut.HandleAsync(Comando(9, 30, 10, 30));

        result.Value!.Motivo.Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    [Fact]
    public async Task TC_DISP_003_Franja_que_termina_en_la_hora_de_cierre_esta_dentro()
    {
        var sut = CrearSut();

        var result = await sut.HandleAsync(Comando(17, 0, 18, 0));

        result.Value!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
    }

    [Fact]
    public async Task TC_DISP_003_Franja_que_empieza_en_la_hora_de_cierre_esta_fuera()
    {
        var sut = CrearSut();

        var result = await sut.HandleAsync(Comando(18, 0, 19, 0));

        result.Value!.Motivo.Should().Be(MotivosDisponibilidad.FueraDeHorario);
    }

    [Fact]
    public async Task TC_DISP_003_La_comparacion_se_hace_en_UTC()
    {
        var sut = CrearSut();
        Reservas(Reserva(10, 0, 11, 0, "APROBADA"));
        var inicioUtc = new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
        var finUtc = inicioUtc.AddHours(1);

        var result = await sut.HandleAsync(new ConsultarDisponibilidadCommand(
            FechaSpace, inicioUtc, finUtc));

        result.Value!.Motivo.Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    private static readonly Guid FechaSpace = Guid.NewGuid();

    private ConsultarDisponibilidad CrearSut(bool activo = true)
    {
        var space = new Space(
            FechaSpace,
            "Laboratorio A-101",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)),
            activo);
        _spaces.Setup(x => x.GetByIdAsync(FechaSpace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(space);
        _reservas.Setup(x => x.ObtenerReservasAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReservaBloqueante>());
        return new ConsultarDisponibilidad(_spaces.Object, _reservas.Object);
    }

    private void Reservas(params ReservaBloqueante[] reservas) =>
        _reservas.Setup(x => x.ObtenerReservasAsync(
                FechaSpace, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservas);

    private static ConsultarDisponibilidadCommand Comando(int inicioHora, int inicioMin, int finHora, int finMin) =>
        new(FechaSpace, Bogota(2026, 11, 20, inicioHora, inicioMin), Bogota(2026, 11, 20, finHora, finMin));

    private static ReservaBloqueante Reserva(int inicioHora, int inicioMin, int finHora, int finMin, string estado) =>
        new(Guid.NewGuid(), FechaSpace, estado,
            Bogota(2026, 11, 20, inicioHora, inicioMin),
            Bogota(2026, 11, 20, finHora, finMin));

    private static DateTimeOffset Bogota(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, TimeSpan.FromHours(-5));
}
