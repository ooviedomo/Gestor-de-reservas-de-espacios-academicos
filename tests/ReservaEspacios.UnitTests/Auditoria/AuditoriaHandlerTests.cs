using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Auditoria;

/// <summary>AUD-001/002 y Observer/Command/Strategy de ADR-002.</summary>
public sealed class AuditoriaHandlerTests
{
    private readonly Guid _entidadId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _ahora = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Mock<IAuditoriaRepository> _repository = new();
    private AuditoriaRegistro? _registro;

    public AuditoriaHandlerTests() =>
        _repository.Setup(x => x.AppendAsync(
                It.IsAny<AuditoriaRegistro>(), It.IsAny<CancellationToken>()))
            .Callback<AuditoriaRegistro, CancellationToken>((registro, _) => _registro = registro)
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task TC_AUD_001_Aprobacion_genera_registro_de_Reservation_con_actor_y_rol()
    {
        await new AuditoriaHandler(_repository.Object).HandleAsync(
            new ReservaAprobada(
                _entidadId, Guid.NewGuid(), Guid.NewGuid(), _actorId, Rol.Coordinador,
                _ahora, _ahora));

        _registro.Should().NotBeNull();
        _registro!.Entidad.Should().Be("Reservation");
        _registro.EntidadId.Should().Be(_entidadId);
        _registro.Accion.Should().Be("APROBADA");
        _registro.ActorId.Should().Be(_actorId);
        _registro.ActorRol.Should().Be("COORDINADOR");
        _registro.OcurridoEn.Should().Be(_ahora);
    }

    [Fact]
    public async Task TC_AUD_001_Cambio_de_rol_genera_registro_de_User()
    {
        await new AuditoriaHandler(_repository.Object).HandleAsync(
            new RolCambiado(
                _entidadId, Rol.Administrador, _actorId, Rol.Administrador, _ahora));

        _registro!.Entidad.Should().Be("User");
        _registro.Accion.Should().Be("ROL_CAMBIADO");
        _registro.Metadata.Should().Contain("ADMINISTRADOR");
    }

    [Fact]
    public async Task TC_AUD_001_Creacion_y_completado_tambien_se_registran()
    {
        var handler = new AuditoriaHandler(_repository.Object);

        await handler.HandleAsync(new ReservaCreada(
            _entidadId, Guid.NewGuid(), Guid.NewGuid(), _actorId, Rol.Solicitante,
            _ahora, _ahora.AddHours(1), _ahora));
        _registro!.Accion.Should().Be("CREADA");

        await handler.HandleAsync(new ReservaCompletada(
            _entidadId, Guid.NewGuid(), Guid.NewGuid(), null, null,
            _ahora, Automatico: true, _ahora));
        _registro!.Accion.Should().Be("COMPLETADA");
        _registro.ActorId.Should().BeNull();
        _registro.ActorRol.Should().BeNull();
    }

    [Theory]
    [InlineData("rechazo", "RECHAZADA", "Espacio en mantención")]
    [InlineData("cancelación", "CANCELADA", "Cambio de planes")]
    public async Task TC_AUD_001_Rechazo_y_cancelacion_conservan_el_motivo(
        string caso, string accion, string motivo)
    {
        var handler = new AuditoriaHandler(_repository.Object);

        if (caso == "rechazo")
        {
            await handler.HandleAsync(new ReservaRechazada(
                _entidadId, Guid.NewGuid(), Guid.NewGuid(), _actorId, Rol.Coordinador,
                motivo, _ahora));
        }
        else
        {
            await handler.HandleAsync(new ReservaCancelada(
                _entidadId, Guid.NewGuid(), Guid.NewGuid(), _actorId, Rol.Solicitante,
                motivo, _ahora));
        }

        _registro!.Accion.Should().Be(accion);
        _registro.Metadata.Should().Contain(motivo);
    }

    [Fact]
    public async Task TC_AUD_001_Higher_de_usuario_sin_actor_queda_como_sistema()
    {
        await new AuditoriaHandler(_repository.Object).HandleAsync(
            new UsuarioCreado(
                _entidadId, "Ana", "ana@example.com", Rol.Solicitante,
                null, null, _ahora));

        _registro!.Entidad.Should().Be("User");
        _registro.ActorId.Should().BeNull();
        _registro.Metadata.Should().Contain("ana@example.com");
    }

    [Fact]
    public async Task TC_AUD_001_Evento_desconocido_no_escribe_registro()
    {
        await new AuditoriaHandler(_repository.Object).HandleAsync(new EventoDesconocido(_ahora));

        _repository.Verify(
            x => x.AppendAsync(It.IsAny<AuditoriaRegistro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_AUD_002_Consulta_devuelve_registros_filtrados()
    {
        var esperados = new[] { new AuditoriaDto(
            _entidadId, "Reservation", _entidadId, "APROBADA", _actorId,
            "Coordinador", "Coordinador de espacios", _ahora, null) };
        var filtro = new FiltroAuditoria("Reservation", _actorId, _ahora, _ahora.AddHours(1));
        _repository.Setup(x => x.ConsultarAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperados);
        var sut = CrearConsulta(Rol.Administrador);

        var resultado = await sut.HandleAsync(filtro);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEquivalentTo(esperados);
        _repository.Verify(x => x.ConsultarAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Rol.Solicitante)]
    public async Task TC_AUD_002_Solicitante_no_puede_consultar_la_auditoria(Rol rol)
    {
        var sut = CrearConsulta(rol);

        var resultado = await sut.HandleAsync(new FiltroAuditoria());

        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _repository.Verify(
            x => x.ConsultarAsync(It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_AUD_002_Coordinador_tambien_puede_consultar()
    {
        _repository.Setup(x => x.ConsultarAsync(
                It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await CrearConsulta(Rol.Coordinador).HandleAsync(new FiltroAuditoria());

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TC_AUD_001_Intento_de_eliminacion_devuelve_AUDITORIA_INMUTABLE()
    {
        _repository.Setup(x => x.IntentarEliminarAsync(_entidadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(DomainErrors.AuditoriaInmutable));
        var sut = new EliminarAuditoria(
            _repository.Object,
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(Rol.Administrador)),
            Usuario(Rol.Administrador));

        var resultado = await sut.HandleAsync(new EliminarAuditoriaCommand(_entidadId));

        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Be(DomainErrors.AuditoriaInmutable);
    }

    [Fact]
    public async Task TC_AUD_001_Solicitante_no_puede_intentar_eliminar()
    {
        var sut = new EliminarAuditoria(
            _repository.Object,
            new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(Rol.Solicitante)),
            Usuario(Rol.Solicitante));

        var resultado = await sut.HandleAsync(new EliminarAuditoriaCommand(_entidadId));

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
    }

    private ConsultarAuditoria CrearConsulta(Rol rol) => new(
        _repository.Object,
        new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(rol)));

    private IUsuarioActual Usuario(Rol rol) => new UsuarioDePrueba(rol, _actorId);

    private sealed record EventoDesconocido(DateTimeOffset OcurridoEn) : IDomainEvent;

    private sealed record UsuarioDePrueba(Rol Rol, Guid? UsuarioId) : IUsuarioActual;
}
