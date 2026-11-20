using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Identity;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>USR-001/002/003 ejecutados contra ASP.NET Core Identity y PostgreSQL local.</summary>
public sealed class IdentityUsuarioServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_USR_001_Registro_valido_crea_usuario_activo_con_rol_solicitante()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var (identity, userManager) = Servicios(scope);
        var sut = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = Email();

        var result = await sut.HandleAsync(new RegistrarSolicitanteCommand("Ana", email, "Segura123"));

        result.IsSuccess.Should().BeTrue();
        var usuario = await userManager.FindByEmailAsync(email);
        usuario.Should().NotBeNull();
        usuario!.UserName.Should().Be(email);
        usuario.Activo.Should().BeTrue();
        (await userManager.GetRolesAsync(usuario)).Should().ContainSingle()
            .Which.Should().Be(Rol.Solicitante.Codigo());
    }

    [Fact]
    public async Task TC_USR_001_Email_duplicado_se_rechaza()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = Email();
        await sut.HandleAsync(new RegistrarSolicitanteCommand("Ana", email, "Segura123"));

        var segundo = await sut.HandleAsync(new RegistrarSolicitanteCommand("Ana 2", email, "Segura123"));

        segundo.ErrorCode.Should().Be(DomainErrors.EmailDuplicado);
    }

    [Fact]
    public async Task TC_USR_001_Contrasena_debil_se_rechaza()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();

        var result = await sut.HandleAsync(new RegistrarSolicitanteCommand(
            "Ana", Email(), "solo_minusculas"));

        result.ErrorCode.Should().Be(DomainErrors.ContrasenaDebil);
    }

    [Fact]
    public async Task TC_USR_002_Administrador_crea_coordinador_activo()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var (identity, userManager) = Servicios(scope);
        var sut = new CrearUsuarioPrivilegiado(identity, Usuario(Rol.Administrador));
        var email = Email();

        var result = await sut.HandleAsync(new CrearUsuarioPrivilegiadoCommand(
            "Luis", email, "Segura123", Rol.Coordinador));

        result.IsSuccess.Should().BeTrue();
        var usuario = await userManager.FindByEmailAsync(email);
        (await userManager.GetRolesAsync(usuario!)).Should().ContainSingle()
            .Which.Should().Be(Rol.Coordinador.Codigo());
    }

    [Fact]
    public async Task TC_USR_002_Desactivar_cuenta_impide_iniciar_sesion()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var (identity, _) = Servicios(scope);
        var registrar = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = Email();
        var creacion = await registrar.HandleAsync(
            new RegistrarSolicitanteCommand("Ana", email, "Segura123"));

        await identity.ActivarDesactivarAsync(creacion.Value, false);
        var login = await identity.AutenticarAsync(email, "Segura123");

        login.ErrorCode.Should().Be(DomainErrors.CuentaDesactivada);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TC_USR_003_Credenciales_invalidas_no_revelan_campo_fallido(
        bool emailInexistente, bool contrasenaIncorrecta)
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var (identity, _) = Servicios(scope);
        var registrar = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = Email();
        await registrar.HandleAsync(new RegistrarSolicitanteCommand("Ana", email, "Segura123"));

        var login = await identity.AutenticarAsync(
            emailInexistente ? Email() : email,
            contrasenaIncorrecta ? "incorrecta" : "Segura123");

        login.ErrorCode.Should().Be(DomainErrors.CredencialesInvalidas);
    }

    [Fact]
    public async Task TC_USR_003_Inicio_sesion_valido_devuelve_sesion_con_rol()
    {
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var (identity, _) = Servicios(scope);
        var registrar = scope.ServiceProvider.GetRequiredService<RegistrarSolicitante>();
        var email = Email();
        await registrar.HandleAsync(new RegistrarSolicitanteCommand("Ana", email, "Segura123"));

        var sesion = await identity.AutenticarAsync(email, "Segura123");

        sesion.IsSuccess.Should().BeTrue();
        sesion.Value!.Rol.Should().Be(Rol.Solicitante);
        sesion.Value.Email.Should().Be(email);
    }

    private WebApplicationFactory<Program> CrearFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString));

    private static (IUsuarioIdentityService, UserManager<ApplicationUser>) Servicios(
        IServiceScope scope)
    {
        // SignInManager necesita un HttpContext para emitir la cookie de sesión.
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        return (
            scope.ServiceProvider.GetRequiredService<IUsuarioIdentityService>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    private static IUsuarioActual Usuario(Rol rol) => new UsuarioActualStub(rol);

    private static string Email() => $"usr-{Guid.NewGuid():N}@universidad.edu";

    private sealed class UsuarioActualStub(Rol rol) : IUsuarioActual
    {
        public Rol Rol { get; } = rol;
        public Guid? UsuarioId => Guid.NewGuid();
    }
}
