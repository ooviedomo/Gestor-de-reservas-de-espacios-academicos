using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Usuarios;

/// <summary>
/// AUD-001 para usuarios: los casos de uso publican el evento que el Observer de
/// auditoría persiste. El actor es nulo en el autorregistro y administrador en el resto.
/// </summary>
public sealed class EventosUsuarioTests
{
    private readonly Guid _administradorId = Guid.NewGuid();
    private readonly Mock<IUsuarioIdentityService> _identity = new();
    private readonly Mock<IDomainEventDispatcher> _eventos = new();
    private IDomainEvent? _publicado;

    public EventosUsuarioTests() =>
        _eventos.Setup(x => x.PublicarAsync(
                It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IDomainEvent, CancellationToken>((evento, _) => _publicado = evento)
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task TC_AUD_001_Alta_de_solicitante_publica_UsuarioCreado_sin_actor()
    {
        var id = Guid.NewGuid();
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(id));
        var sut = new RegistrarSolicitante(_identity.Object, _eventos.Object);

        await sut.HandleAsync(new RegistrarSolicitanteCommand(
            "Ana", "ana@universidad.edu", "Segura123"));

        var evento = _publicado.Should().BeOfType<UsuarioCreado>().Subject;
        evento.UsuarioId.Should().Be(id);
        evento.Email.Should().Be("ana@universidad.edu");
        evento.Rol.Should().Be(Rol.Solicitante);
        evento.ActorId.Should().BeNull();
        evento.ActorRol.Should().BeNull();
    }

    [Fact]
    public async Task TC_AUD_001_Alta_privilegiada_registra_al_administrador_como_actor()
    {
        var id = Guid.NewGuid();
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(id));
        var sut = new CrearUsuarioPrivilegiado(
            _identity.Object, Usuario(Rol.Administrador), _eventos.Object);

        await sut.HandleAsync(new CrearUsuarioPrivilegiadoCommand(
            "Luis", "luis@universidad.edu", "Segura123", Rol.Coordinador));

        var evento = _publicado.Should().BeOfType<UsuarioCreado>().Subject;
        evento.Rol.Should().Be(Rol.Coordinador);
        evento.ActorId.Should().Be(_administradorId);
        evento.ActorRol.Should().Be(Rol.Administrador);
    }

    [Fact]
    public async Task TC_AUD_001_Cambio_de_rol_publica_RolCambiado_con_el_nuevo_rol()
    {
        var usuarioId = Guid.NewGuid();
        _identity.Setup(x => x.CambiarRolAsync(usuarioId, Rol.Coordinador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var sut = new CambiarRolUsuario(
            _identity.Object, Usuario(Rol.Administrador), _eventos.Object);

        await sut.HandleAsync(new CambiarRolUsuarioCommand(usuarioId, Rol.Coordinador));

        var evento = _publicado.Should().BeOfType<RolCambiado>().Subject;
        evento.UsuarioId.Should().Be(usuarioId);
        evento.RolNuevo.Should().Be(Rol.Coordinador);
        evento.ActorId.Should().Be(_administradorId);
    }

    [Fact]
    public async Task TC_AUD_001_Operacion_rechazada_no_publica_evento()
    {
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(DomainErrors.EmailDuplicado));

        await new RegistrarSolicitante(_identity.Object, _eventos.Object)
            .HandleAsync(new RegistrarSolicitanteCommand("Ana", "ana@universidad.edu", "Segura123"));

        _eventos.Verify(
            x => x.PublicarAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_AUD_001_Solicitante_no_cambia_roles_ni_publica_evento()
    {
        var sut = new CambiarRolUsuario(
            _identity.Object, Usuario(Rol.Solicitante), _eventos.Object);

        await sut.HandleAsync(new CambiarRolUsuarioCommand(Guid.NewGuid(), Rol.Administrador));

        _eventos.Verify(
            x => x.PublicarAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private IUsuarioActual Usuario(Rol rol) => new UsuarioDePrueba(rol, _administradorId);

    private sealed record UsuarioDePrueba(Rol Rol, Guid? UsuarioId) : IUsuarioActual;
}
