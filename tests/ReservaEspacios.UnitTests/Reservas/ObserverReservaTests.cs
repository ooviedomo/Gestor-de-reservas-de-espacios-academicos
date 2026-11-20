using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Notificaciones;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// Observer (ADR-002, tarea 5.3): el despachador propio entrega cada evento a todos los
/// suscriptores y los comandos publican el evento correcto tras persistir la transición.
/// No se añade MediatR a la stack del plan.
/// </summary>
public sealed class ObserverReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Guid _reservaId = Guid.NewGuid();
    private readonly Guid _espacioId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Guid _coordinadorId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();
    private readonly Mock<IDomainEventDispatcher> _eventos = new();
    private IDomainEvent? _publicado;

    public ObserverReservaTests()
    {
        _eventos.Setup(x => x.PublicarAsync(
                It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IDomainEvent, CancellationToken>((e, _) => _publicado = e)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task TC_OBS_001_Despachador_entrega_el_evento_a_todos_los_suscriptores()
    {
        var primero = new Mock<IDomainEventSubscriber>();
        var segundo = new Mock<IDomainEventSubscriber>();
        primero.Setup(x => x.HandleAsync(
                It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        segundo.Setup(x => x.HandleAsync(
                It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var evento = new ReservaAprobada(
            _reservaId, _espacioId, _solicitanteId, _coordinadorId, Rol.Coordinador,
            Inicio, Inicio);
        var sut = new DomainEventDispatcher([primero.Object, segundo.Object]);

        await sut.PublicarAsync(evento);

        primero.Verify(x => x.HandleAsync(evento, It.IsAny<CancellationToken>()), Times.Once);
        segundo.Verify(x => x.HandleAsync(evento, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_OBS_002_Aprobar_publica_ReservaAprobada_con_actor_y_espacio()
    {
        DadoQueExiste(NuevaPendiente());

        var resultado = await CrearAprobador().HandleAsync(
            new AprobarReservaCommand(_reservaId));

        resultado.IsSuccess.Should().BeTrue();
        var evento = _publicado.Should().BeOfType<ReservaAprobada>().Subject;
        evento.ReservaId.Should().Be(_reservaId);
        evento.EspacioId.Should().Be(_espacioId);
        evento.SolicitanteId.Should().Be(_solicitanteId);
        evento.ActorId.Should().Be(_coordinadorId);
        evento.ActorRol.Should().Be(Rol.Coordinador);
        evento.AprobadoEn.Should().NotBe(default);
    }

    [Fact]
    public async Task TC_OBS_003_Transicion_invalida_no_publica_evento()
    {
        var reserva = NuevaPendiente();
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador));
        DadoQueExiste(reserva);

        var resultado = await CrearAprobador().HandleAsync(
            new AprobarReservaCommand(_reservaId));

        resultado.ErrorCode.Should().NotBeNull();
        _eventos.Verify(x => x.PublicarAsync(
            It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_OBS_004_Cierre_automatico_publica_ReservaCompletada_sin_actor()
    {
        var reserva = NuevaPendiente();
        var momento = Inicio.AddHours(2);
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador), Inicio);
        _repository.Setup(x => x.ObtenerAprobadasVencidasAsync(
                momento, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reserva]);
        var sut = new CerrarReservasVencidas(_repository.Object, _eventos.Object);

        var completadas = await sut.EjecutarAsync(momento);

        completadas.Should().Be(1);
        var evento = _publicado.Should().BeOfType<ReservaCompletada>().Subject;
        evento.Automatico.Should().BeTrue();
        evento.ActorId.Should().BeNull();
        evento.ActorRol.Should().BeNull();
        evento.CompletadoEn.Should().Be(momento);
    }

    [Fact]
    public void TC_OBS_005_Application_resuelve_el_despachador_con_auditoria_y_outbox()
    {
        var servicios = new ServiceCollection();
        servicios.AddApplication();
        servicios.AddSingleton(Mock.Of<IAuditoriaRepository>());
        servicios.AddSingleton(Mock.Of<IReservaRepository>());
        servicios.AddSingleton(Mock.Of<ISpaceRepository>());
        servicios.AddSingleton(Mock.Of<IUsuarioIdentityService>());
        servicios.AddSingleton(Mock.Of<INotificacionRepository>());
        using var proveedor = servicios.BuildServiceProvider();
        using var scope = proveedor.CreateScope();

        var despachador = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var suscriptores = scope.ServiceProvider
            .GetServices<IDomainEventSubscriber>().ToList();

        despachador.Should().BeOfType<DomainEventDispatcher>();
        suscriptores.Should().HaveCount(2);
        suscriptores.Should().ContainItemsAssignableTo<AuditoriaHandler>();
        suscriptores.Should().ContainItemsAssignableTo<NotificacionOutboxHandler>();
    }

    private void DadoQueExiste(Reservation reserva) =>
        _repository.Setup(x => x.GetByIdAsync(_reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

    private AprobarReserva CrearAprobador()
    {
        IUsuarioActual usuario = new UsuarioAutenticado(Rol.Coordinador, _coordinadorId);
        return new AprobarReserva(
            _repository.Object,
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), usuario),
            usuario,
            _eventos.Object);
    }

    private Reservation NuevaPendiente() => new(
        _reservaId, _espacioId, _solicitanteId, Inicio, Inicio.AddHours(1),
        "Clase de laboratorio", 20);

    private sealed class UsuarioAutenticado(Rol rol, Guid id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
