using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Usuarios;

public sealed class UsuarioUseCasesTests
{
    private readonly Mock<IUsuarioIdentityService> _identity = new();

    [Fact]
    public async Task TC_USR_001_Registro_valido_crea_cuenta_de_solicitante()
    {
        var id = Guid.NewGuid();
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(id));
        var sut = new RegistrarSolicitante(_identity.Object);

        var result = await sut.HandleAsync(new RegistrarSolicitanteCommand(
            "Ana", "ana@universidad.edu", "Segura123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
        _identity.Verify(x => x.CrearAsync(
            It.Is<CrearUsuarioCommand>(c =>
                c.Rol == Rol.Solicitante &&
                c.Email == "ana@universidad.edu" &&
                c.Contrasena == "Segura123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DomainErrors.EmailDuplicado)]
    [InlineData(DomainErrors.ContrasenaDebil)]
    public async Task TC_USR_001_Error_de_identidad_se_propaga_al_cliente(string codigo)
    {
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(codigo));
        var sut = new RegistrarSolicitante(_identity.Object);

        var result = await sut.HandleAsync(new RegistrarSolicitanteCommand(
            "Ana", "ana@universidad.edu", "solo_minusculas"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(codigo);
    }

    [Fact]
    public async Task TC_USR_002_Administrador_crea_coordinador_activo()
    {
        var id = Guid.NewGuid();
        _identity.Setup(x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(id));
        var sut = new CrearUsuarioPrivilegiado(_identity.Object, Usuario(Rol.Administrador));

        var result = await sut.HandleAsync(new CrearUsuarioPrivilegiadoCommand(
            "Luis", "luis@universidad.edu", "Segura123", Rol.Coordinador));

        result.IsSuccess.Should().BeTrue();
        _identity.Verify(x => x.CrearAsync(
            It.Is<CrearUsuarioCommand>(c => c.Rol == Rol.Coordinador),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_USR_002_Solicitante_no_puede_crear_coordinador()
    {
        var sut = new CrearUsuarioPrivilegiado(_identity.Object, Usuario(Rol.Solicitante));

        var result = await sut.HandleAsync(new CrearUsuarioPrivilegiadoCommand(
            "Luis", "luis@universidad.edu", "Segura123", Rol.Coordinador));

        result.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _identity.Verify(
            x => x.CrearAsync(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_USR_002_Solicitante_no_puede_cambiar_roles()
    {
        var sut = new CambiarRolUsuario(_identity.Object, Usuario(Rol.Coordinador));

        var result = await sut.HandleAsync(new CambiarRolUsuarioCommand(Guid.NewGuid(), Rol.Administrador));

        result.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _identity.Verify(
            x => x.CambiarRolAsync(It.IsAny<Guid>(), It.IsAny<Rol>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TC_USR_002_Desactivar_cuenta()
    {
        _identity.Setup(x => x.ActivarDesactivarAsync(
                It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var id = Guid.NewGuid();
        var sut = new ActivarDesactivarUsuario(_identity.Object, Usuario(Rol.Administrador));

        var result = await sut.HandleAsync(new ActivarDesactivarUsuarioCommand(id, false));

        result.IsSuccess.Should().BeTrue();
        _identity.Verify(x => x.ActivarDesactivarAsync(id, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_USR_003_Inicio_sesion_valido_devuelve_sesion_con_su_rol()
    {
        var sesion = new UsuarioSesionDto(
            Guid.NewGuid(), "Ana", "ana@universidad.edu", Rol.Solicitante, true);
        _identity.Setup(x => x.AutenticarAsync(
                "ana@universidad.edu", "Segura123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UsuarioSesionDto>.Success(sesion));
        var sut = new AutenticarUsuario(_identity.Object);

        var result = await sut.HandleAsync(new AutenticarUsuarioCommand(
            "ana@universidad.edu", "Segura123"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rol.Should().Be(Rol.Solicitante);
    }

    [Theory]
    [InlineData(DomainErrors.CredencialesInvalidas)]
    [InlineData(DomainErrors.CuentaDesactivada)]
    public async Task TC_USR_003_Autenticacion_rechazada_devuelve_el_codigo_del_dominio(string codigo)
    {
        _identity.Setup(x => x.AutenticarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UsuarioSesionDto>.Failure(codigo));
        var sut = new AutenticarUsuario(_identity.Object);

        var result = await sut.HandleAsync(new AutenticarUsuarioCommand(
            "ana@universidad.edu", "incorrecta"));

        result.ErrorCode.Should().Be(codigo);
    }

    private static IUsuarioActual Usuario(Rol rol) => new UsuarioActualStub(rol);

    private sealed class UsuarioActualStub(Rol rol) : IUsuarioActual
    {
        public Rol Rol { get; } = rol;
        public Guid? UsuarioId => Guid.NewGuid();
    }
}
