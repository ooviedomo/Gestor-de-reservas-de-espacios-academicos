using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// RES-004 (tarea 4.3): verifica alcance por rol, propagación de filtros y que el
/// historial exponga nombre/rol del actor y motivos en lenguaje de dominio.
/// </summary>
public sealed class ConsultarHistorialTests
{
    private static readonly DateTimeOffset CreadoEn =
        new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AprobadoEn =
        new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);
    private readonly Guid _espacioId = Guid.NewGuid();
    private readonly Guid _solicitanteId = Guid.NewGuid();
    private readonly Guid _otroSolicitanteId = Guid.NewGuid();
    private readonly Guid _coordinadorId = Guid.NewGuid();
    private readonly Mock<IReservaRepository> _repository = new();
    private readonly Mock<ISpaceRepository> _spaces = new();
    private readonly Mock<IUsuarioIdentityService> _identity = new();
    private readonly Dictionary<Guid, DatosActorDto> _datos = [];

    public ConsultarHistorialTests()
    {
        _spaces.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Space(
                    _espacioId, "Laboratorio A-101", EspacioTipo.Laboratorio, 30,
                    "Bloque A", new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)))
            });
        _identity.Setup(x => x.ObtenerPorIdAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                Result<DatosActorDto?>.Success(
                    _datos.TryGetValue(id, out var dato) ? dato : null));
        _datos[_solicitanteId] = new DatosActorDto(
            _solicitanteId, "Ana Pérez", Rol.Solicitante);
        _datos[_otroSolicitanteId] = new DatosActorDto(
            _otroSolicitanteId, "Bruno Ruiz", Rol.Solicitante);
        _datos[_coordinadorId] = new DatosActorDto(
            _coordinadorId, "Laura Gómez", Rol.Coordinador);
    }

    [Fact]
    public async Task TC_RES_004_Solicitante_ve_solo_las_suyas_aunque_filtre_otro_usuario()
    {
        DadoQueElRepositorioDevuelve([Nueva(_solicitanteId)]);
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(new ConsultarHistorialCommand(
            FiltroHistorialConUsuario(_otroSolicitanteId)));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Should().ContainSingle();
        resultado.Value[0].Solicitante.Should().Be("Ana Pérez");
        _repository.Verify(x => x.ObtenerHistorialAsync(
            It.Is<FiltroHistorial>(f => f.UsuarioId == _solicitanteId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_RES_004_Coordinador_ve_todas_las_reservas()
    {
        DadoQueElRepositorioDevuelve(
            [Nueva(_solicitanteId), Nueva(_otroSolicitanteId)]);
        var sut = CrearSut(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(
            new ConsultarHistorialCommand(new FiltroHistorial()));

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(2);
        resultado.Value.Select(x => x.Solicitante).Should()
            .BeEquivalentTo(["Ana Pérez", "Bruno Ruiz"]);
        _repository.Verify(x => x.ObtenerHistorialAsync(
            It.Is<FiltroHistorial>(f => f.UsuarioId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_RES_004_Filtros_de_estado_fecha_y_espacio_se_reenvian()
    {
        var filtro = new FiltroHistorial(
            Estado: EstadoReserva.Cancelada,
            Desde: Inicio.AddDays(-1),
            Hasta: Inicio.AddDays(1),
            SpaceId: _espacioId);
        DadoQueElRepositorioDevuelve([]);
        var sut = CrearSut(Rol.Administrador, Guid.NewGuid());

        await sut.HandleAsync(new ConsultarHistorialCommand(filtro));

        _repository.Verify(x => x.ObtenerHistorialAsync(
            It.Is<FiltroHistorial>(f =>
                f.Estado == EstadoReserva.Cancelada &&
                f.Desde == filtro.Desde &&
                f.Hasta == filtro.Hasta &&
                f.SpaceId == _espacioId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_RES_004_Decision_registrada_muestra_nombre_y_rol_del_actor()
    {
        var reserva = Nueva(_solicitanteId);
        reserva.Aprobar(ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador), AprobadoEn);
        DadoQueElRepositorioDevuelve([reserva]);
        var sut = CrearSut(Rol.Coordinador, _coordinadorId);

        var resultado = await sut.HandleAsync(
            new ConsultarHistorialCommand(new FiltroHistorial()));

        var fila = resultado.Value!.Should().ContainSingle().Subject;
        fila.Estado.Should().Be("APROBADA");
        fila.Actor.Should().Be("Laura Gómez");
        fila.RolActor.Should().Be("Coordinador de espacios");
        fila.AprobadoEn.Should().Be(AprobadoEn);
        fila.Espacio.Should().Be("Laboratorio A-101");
    }

    [Fact]
    public async Task TC_RES_004_Motivo_de_rechazo_aparece_en_el_historial()
    {
        var reserva = Nueva(_solicitanteId);
        reserva.Rechazar(
            ActorTransicion.Usuario(_coordinadorId, Rol.Coordinador),
            "Espacio en mantención", AprobadoEn);
        DadoQueElRepositorioDevuelve([reserva]);
        var sut = CrearSut(Rol.Solicitante, _solicitanteId);

        var resultado = await sut.HandleAsync(
            new ConsultarHistorialCommand(new FiltroHistorial()));

        var fila = resultado.Value!.Should().ContainSingle().Subject;
        fila.Estado.Should().Be("RECHAZADA");
        fila.MotivoRechazo.Should().Be("Espacio en mantención");
        fila.Actor.Should().Be("Laura Gómez");
        fila.RolActor.Should().Be("Coordinador de espacios");
    }

    [Fact]
    public async Task TC_RES_004_Sin_autenticacion_devuelve_NO_AUTENTICADO()
    {
        IUsuarioActual anonimo = new UsuarioAutenticado(Rol.Solicitante, null);
        var sut = new ConsultarHistorial(
            _repository.Object,
            _spaces.Object,
            _identity.Object,
            new EvaluadorPermisos(),
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), anonimo),
            anonimo);

        var resultado = await sut.HandleAsync(
            new ConsultarHistorialCommand(new FiltroHistorial()));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutenticado);
        _repository.Verify(x => x.ObtenerHistorialAsync(
            It.IsAny<FiltroHistorial>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void DadoQueElRepositorioDevuelve(IReadOnlyList<Reservation> reservas) =>
        _repository.Setup(x => x.ObtenerHistorialAsync(
                It.IsAny<FiltroHistorial>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservas);

    private ConsultarHistorial CrearSut(Rol rol, Guid usuarioId)
    {
        IUsuarioActual usuario = new UsuarioAutenticado(rol, usuarioId);
        var evaluador = new EvaluadorPermisos();
        return new ConsultarHistorial(
            _repository.Object,
            _spaces.Object,
            _identity.Object,
            evaluador,
            new GuardiaOperacionesProtegidas(evaluador, usuario),
            usuario);
    }

    private static FiltroHistorial FiltroHistorialConUsuario(Guid usuarioId) =>
        new(UsuarioId: usuarioId);

    private Reservation Nueva(Guid usuarioId) => new(
        Guid.NewGuid(), _espacioId, usuarioId, Inicio, Inicio.AddHours(1),
        "Clase de laboratorio", 20, CreadoEn);

    private sealed class UsuarioAutenticado(Rol rol, Guid? id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
