using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// RES-003/AUTH-002 (tarea 3.4): el comando comprueba autenticación y propiedad antes
/// de delegar en la máquina de estados, y persiste la transición con su motivo.
/// </summary>
public sealed class CancelarReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Guid _reservaId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Guid _otroSolicitanteId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();

    [Fact]
    public async Task TC_RES_003_Solicitante_cancela_la_suya_y_persiste_motivo_y_actor()
    {
        var reserva = DadoQueExiste(Nueva(_solicitanteId, EstadoReserva.Aprobada));
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            _reservaId, "Docencia reprogramada"));

        resultado.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.MotivoCancelacion.Should().Be("Docencia reprogramada");
        reserva.ActorId.Should().Be(_solicitanteId);
        reserva.CanceladoEn.Should().NotBeNull();
        _repository.Verify(x => x.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_RES_003_Coordinador_cancela_reserva_ajena()
    {
        var coordinadorId = Guid.NewGuid();
        var reserva = DadoQueExiste(Nueva(_otroSolicitanteId));
        var sut = CrearSut(Rol.Coordinador, coordinadorId);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            _reservaId, "Espacio fuera de servicio"));

        resultado.IsSuccess.Should().BeTrue();
        reserva.ActorId.Should().Be(coordinadorId);
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task TC_RES_003_Solicitante_no_cancela_la_de_otro_devuelve_NO_AUTORIZADO()
    {
        DadoQueExiste(Nueva(_otroSolicitanteId));
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            _reservaId, "Docencia reprogramada"));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TC_RES_003_Cancelacion_sin_motivo_devuelve_MOTIVO_OBLIGATORIO(string motivo)
    {
        DadoQueExiste(Nueva(_solicitanteId));
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(_reservaId, motivo));

        resultado.ErrorCode.Should().Be(DomainErrors.MotivoObligatorio);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_RES_003_Cancelar_reserva_rechazada_devuelve_TRANSICION_INVALIDA()
    {
        DadoQueExiste(Nueva(_solicitanteId, EstadoReserva.Rechazada));
        var sut = CrearSut(Rol.Coordinador, Guid.NewGuid());

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            _reservaId, "Motivo"));

        resultado.ErrorCode.Should().Be(DomainErrors.TransicionInvalida);
    }

    [Fact]
    public async Task TC_RES_003_Reserva_inexistente_devuelve_RESERVA_NO_ENCONTRADA()
    {
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            Guid.NewGuid(), "Motivo"));

        resultado.ErrorCode.Should().Be(DomainErrors.ReservaNoEncontrada);
    }

    [Fact]
    public async Task TC_RES_003_Sin_autenticacion_devuelve_NO_AUTENTICADO()
    {
        IUsuarioActual anonimo = new UsuarioAutenticado(Rol.Solicitante, null);
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), anonimo);
        var sut = new CancelarReserva(_repository.Object, guardia, anonimo);

        var resultado = await sut.HandleAsync(new CancelarReservaCommand(
            _reservaId, "Motivo"));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutenticado);
        _repository.Verify(x => x.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Reservation DadoQueExiste(Reservation reserva)
    {
        _repository.Setup(x => x.GetByIdAsync(_reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        return reserva;
    }

    private CancelarReserva CrearSut(Rol rol, Guid usuarioId)
    {
        IUsuarioActual usuario = new UsuarioAutenticado(rol, usuarioId);
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), usuario);
        return new CancelarReserva(_repository.Object, guardia, usuario);
    }

    private Reservation Nueva(Guid usuarioId, EstadoReserva estado = EstadoReserva.Pendiente)
    {
        var reserva = new Reservation(
            _reservaId, Guid.NewGuid(), usuarioId, Inicio, Inicio.AddHours(1), "Clase", 20);
        var coordinador = ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador);

        switch (estado)
        {
            case EstadoReserva.Aprobada:
                reserva.Aprobar(coordinador);
                break;
            case EstadoReserva.Rechazada:
                reserva.Rechazar(coordinador, "Mantenimiento");
                break;
        }

        return reserva;
    }

    private sealed class UsuarioAutenticado(Rol rol, Guid? id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
