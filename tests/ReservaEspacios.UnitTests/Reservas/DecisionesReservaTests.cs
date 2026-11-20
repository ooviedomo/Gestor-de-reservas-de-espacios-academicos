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
/// APR-001 (tarea 4.1): los comandos de aprobación y rechazo aplican la matriz de
/// permisos y delegan la transición y el registro de actor/fecha/motivo en Domain.
/// </summary>
public sealed class DecisionesReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Guid _reservaId = Guid.NewGuid();
    private readonly Guid _coordinadorId = Guid.NewGuid();
    private readonly Guid _administradorId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();

    [Fact]
    public async Task TC_APR_001_Coordinador_aprueba_pendiente_y_registra_actor_y_fecha()
    {
        var reserva = DadoQueExiste(Nueva());
        var sut = CrearAprobador(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(new AprobarReservaCommand(_reservaId));

        resultado.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
        reserva.AprobadoPor.Should().Be(_coordinadorId);
        reserva.AprobadoEn.Should().NotBeNull();
        reserva.ActorId.Should().Be(_coordinadorId);
        _repository.Verify(x => x.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_APR_001_Reaprobar_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = Nueva();
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador));
        DadoQueExiste(reserva);
        var sut = CrearAprobador(Rol.Administrador, _administradorId);

        var resultado = await sut.HandleAsync(new AprobarReservaCommand(_reservaId));

        resultado.ErrorCode.Should().Be(DomainErrors.TransicionInvalida);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_APR_001_Solicitante_no_aprueba_devuelve_NO_AUTORIZADO()
    {
        DadoQueExiste(Nueva());
        var sut = CrearAprobador(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new AprobarReservaCommand(_reservaId));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_APR_001_Administrador_rechaza_con_motivo()
    {
        var reserva = DadoQueExiste(Nueva());
        var sut = CrearRechazador(Rol.Administrador, _administradorId);

        var resultado = await sut.HandleAsync(new RechazarReservaCommand(
            _reservaId, "Espacio en mantención"));

        resultado.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Rechazada);
        reserva.MotivoRechazo.Should().Be("Espacio en mantención");
        reserva.ActorId.Should().Be(_administradorId);
        _repository.Verify(x => x.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TC_APR_001_Rechazo_sin_motivo_devuelve_MOTIVO_OBLIGATORIO(string motivo)
    {
        DadoQueExiste(Nueva());
        var sut = CrearRechazador(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(new RechazarReservaCommand(_reservaId, motivo));

        resultado.ErrorCode.Should().Be(DomainErrors.MotivoObligatorio);
        _repository.Verify(x => x.UpdateAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_APR_001_Rechazar_reserva_aprobada_devuelve_TRANSICION_INVALIDA()
    {
        var reserva = Nueva();
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador));
        DadoQueExiste(reserva);
        var sut = CrearRechazador(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(new RechazarReservaCommand(_reservaId, "Motivo"));

        resultado.ErrorCode.Should().Be(DomainErrors.TransicionInvalida);
    }

    [Fact]
    public async Task TC_APR_001_Reserva_inexistente_devuelve_RESERVA_NO_ENCONTRADA()
    {
        var aprobador = CrearAprobador(Rol.Coordinador, _coordinadorId);
        var rechazador = CrearRechazador(Rol.Administrador, _administradorId);

        (await aprobador.HandleAsync(new AprobarReservaCommand(Guid.NewGuid())))
            .ErrorCode.Should().Be(DomainErrors.ReservaNoEncontrada);
        (await rechazador.HandleAsync(new RechazarReservaCommand(Guid.NewGuid(), "Motivo")))
            .ErrorCode.Should().Be(DomainErrors.ReservaNoEncontrada);
    }

    private Reservation DadoQueExiste(Reservation reserva)
    {
        _repository.Setup(x => x.GetByIdAsync(_reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        return reserva;
    }

    private AprobarReserva CrearAprobador(Rol rol, Guid usuarioId)
    {
        IUsuarioActual usuario = new UsuarioAutenticado(rol, usuarioId);
        return new AprobarReserva(
            _repository.Object, Guardia(usuario), usuario);
    }

    private RechazarReserva CrearRechazador(Rol rol, Guid usuarioId)
    {
        IUsuarioActual usuario = new UsuarioAutenticado(rol, usuarioId);
        return new RechazarReserva(
            _repository.Object, Guardia(usuario), usuario);
    }

    private static GuardiaOperacionesProtegidas Guardia(IUsuarioActual usuario) =>
        new(new EvaluadorPermisos(), usuario);

    private Reservation Nueva() => new(
        _reservaId, Guid.NewGuid(), _solicitanteId, Inicio, Inicio.AddHours(1), "Clase", 20);

    private sealed class UsuarioAutenticado(Rol rol, Guid id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
