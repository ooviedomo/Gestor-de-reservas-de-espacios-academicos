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
/// COM-001 (tarea 4.2): el comando manual aplica la matriz de permisos y registra la
/// marca de tiempo de completado a través de la máquina de estados de Domain.
/// </summary>
public sealed class CompletarReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Guid _reservaId = Guid.NewGuid();
    private readonly Guid _coordinadorId = Guid.NewGuid();
    private readonly Guid _administradorId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public async Task TC_COM_001_Rol_con_permiso_completa_y_registra_fecha(Rol rol)
    {
        var reserva = DadoQueExiste(NuevaAprobada());
        var actorId = rol == Rol.Coordinador ? _coordinadorId : _administradorId;
        var sut = CrearSut(rol, actorId);

        var resultado = await sut.HandleAsync(new CompletarReservaCommand(_reservaId));

        resultado.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Completada);
        reserva.CompletadoEn.Should().NotBeNull();
        reserva.EsBloqueante.Should().BeFalse();
        _repository.Verify(x => x.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_COM_001_Completar_pendiente_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = new Reservation(
            _reservaId, Guid.NewGuid(), _solicitanteId, Inicio, Inicio.AddHours(1), "Clase", 20);
        DadoQueExiste(reserva);
        var sut = CrearSut(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(new CompletarReservaCommand(_reservaId));

        resultado.ErrorCode.Should().Be(DomainErrors.TransicionInvalida);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_COM_001_Solicitante_no_completa_devuelve_NO_AUTORIZADO()
    {
        DadoQueExiste(NuevaAprobada());
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new CompletarReservaCommand(_reservaId));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_COM_001_Reserva_inexistente_devuelve_RESERVA_NO_ENCONTRADA()
    {
        var sut = CrearSut(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(new CompletarReservaCommand(Guid.NewGuid()));

        resultado.ErrorCode.Should().Be(DomainErrors.ReservaNoEncontrada);
    }

    [Fact]
    public async Task TC_COM_001_Sin_autenticacion_devuelve_NO_AUTENTICADO()
    {
        IUsuarioActual anonimo = new UsuarioAutenticado(Rol.Solicitante, null);
        var sut = new CompletarReserva(
            _repository.Object,
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), anonimo),
            anonimo);

        var resultado = await sut.HandleAsync(new CompletarReservaCommand(_reservaId));

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

    private CompletarReserva CrearSut(Rol rol, Guid usuarioId)
    {
        IUsuarioActual usuario = new UsuarioAutenticado(rol, usuarioId);
        return new CompletarReserva(
            _repository.Object,
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), usuario),
            usuario);
    }

    private Reservation NuevaAprobada()
    {
        var reserva = new Reservation(
            _reservaId, Guid.NewGuid(), _solicitanteId, Inicio, Inicio.AddHours(1), "Clase", 20);
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador));
        return reserva;
    }

    private sealed class UsuarioAutenticado(Rol rol, Guid? id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
