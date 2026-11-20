using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Web.Components.Layout;
using ReservaEspacios.Web.Components.Pages;

namespace ReservaEspacios.E2ETests;

/// <summary>
/// UI-001…UI-004: la portada y el menú respetan la matriz canónica y los formularios
/// muestran los códigos estables de los comandos de Application.
/// </summary>
public sealed class InterfazAccesoTests
{
    [Fact]
    public void TC_UI_001_Portada_no_es_el_scaffold_y_el_anonimo_ve_solo_acceso()
    {
        using var ctx = CrearContexto(null);

        var cut = ctx.Render<Home>();
        var texto = cut.Markup;

        texto.Should().NotContain("Hello, world!");
        texto.Should().Contain("Consultar disponibilidad");
        texto.Should().NotContain("Acceder");
        texto.Should().NotContain("registrarse como solicitante");
        texto.Should().NotContain("Solicitar reserva");
        texto.Should().NotContain("Panel de aprobación");
        texto.Should().NotContain("Administrar espacios");
    }

    [Theory]
    [InlineData(Rol.Solicitante, "Solicitar reserva", false, false)]
    [InlineData(Rol.Coordinador, "Panel de aprobación", false, true)]
    [InlineData(Rol.Administrador, "Administrar espacios", true, true)]
    public void TC_UI_001_Portada_muestra_accesos_segun_el_rol(
        Rol rol, string enlaceEsperado, bool administrar, bool aprobar)
    {
        using var ctx = CrearContexto(rol);

        var texto = ctx.Render<Home>().Markup;

        texto.Should().Contain("Ver historial de reservas");
        texto.Should().Contain(enlaceEsperado);
        texto.Should().NotContain("/acceso/login");
        if (aprobar)
        {
            texto.Should().Contain("Panel de aprobación");
        }
        else
        {
            texto.Should().NotContain("Panel de aprobación");
        }
        if (administrar)
        {
            texto.Should().Contain("Administrar espacios");
        }
        else
        {
            texto.Should().NotContain("Administrar espacios");
        }
    }

    [Fact]
    public void TC_UI_001_Menu_anonimo_muestra_login_registro_y_oculta_operaciones()
    {
        using var ctx = CrearContexto(null);

        var texto = ctx.Render<NavMenu>().Markup;

        texto.Should().Contain("/acceso/login");
        texto.Should().Contain("/acceso/registro");
        texto.Should().Contain("/disponibilidad");
        texto.Should().NotContain("/reservas/nueva");
        texto.Should().NotContain("/reservas/aprobaciones");
        texto.Should().NotContain("/administracion/espacios");
    }

    [Theory]
    [InlineData(Rol.Solicitante, "/reservas/nueva", "/reservas/aprobaciones")]
    [InlineData(Rol.Coordinador, "/reservas/aprobaciones", "/administracion/espacios")]
    [InlineData(Rol.Administrador, "/administracion/espacios", "/reservas/nueva")]
    public void TC_UI_001_Menu_filtra_enlaces_y_muestra_la_etiqueta_del_rol(
        Rol rol, string enlaceEsperado, string enlaceOculto)
    {
        using var ctx = CrearContexto(rol);

        var texto = ctx.Render<NavMenu>().Markup;

        texto.Should().Contain(enlaceEsperado);
        texto.Should().NotContain(enlaceOculto);
        texto.Should().Contain(rol.Etiqueta());
        texto.Should().Contain("Cerrar sesión");
    }

    [Fact]
    public void TC_UI_004_Menu_cierra_sesion_hacia_la_portada()
    {
        using var ctx = CrearContexto(Rol.Solicitante);

        var formulario = ctx.Render<NavMenu>().Find("form");

        formulario.GetAttribute("method").Should().Be("post");
        formulario.GetAttribute("action").Should().Be("/acceso/cerrar-sesion");
    }

    [Fact]
    public void TC_UI_002_Login_es_un_formulario_http_normal()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Login>();
        var formulario = cut.Find("form");

        formulario.GetAttribute("method").Should().Be("post");
        formulario.GetAttribute("action").Should().Be("/acceso/autenticar");
        cut.Find("#email").GetAttribute("name").Should().Be("email");
        cut.Find("#contrasena").GetAttribute("name").Should().Be("contrasena");
    }

    [Theory]
    [InlineData("CREDENCIALES_INVALIDAS")]
    [InlineData("CUENTA_DESACTIVADA")]
    public void TC_UI_002_Login_muestra_el_codigo_de_la_query(string codigo)
    {
        using var ctx = new BunitContext();
        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/acceso/login?error={codigo}");

        var cut = ctx.Render<Login>();

        cut.Find("[data-testid='error']").TextContent.Trim().Should().Be(codigo);
    }

    [Fact]
    public void TC_UI_002_Login_ignora_codigos_no_permitidos_en_la_query()
    {
        using var ctx = new BunitContext();
        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/acceso/login?error=EMAIL_DUPLICADO");

        var cut = ctx.Render<Login>();

        cut.FindAll("[data-testid='error']").Should().BeEmpty();
    }

    [Theory]
    [InlineData("EMAIL_DUPLICADO")]
    [InlineData("CONTRASENA_DEBIL")]
    public void TC_UI_003_Registro_muestra_el_codigo_devuelto_por_el_comando(string codigo)
    {
        using var ctx = new BunitContext();
        var registrar = new Mock<ICommandHandler<RegistrarSolicitanteCommand, Guid>>();
        registrar.Setup(x => x.HandleAsync(
                It.IsAny<RegistrarSolicitanteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(codigo));
        ctx.Services.AddSingleton(registrar.Object);

        var cut = ctx.Render<Registro>();
        cut.Find("#nombre").Change("Ana");
        cut.Find("#email").Change("ana@example.com");
        cut.Find("#contrasena").Change("Clave1234");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='error']")
            .TextContent.Trim().Should().Be(codigo));
    }

    [Fact]
    public void TC_UI_003_Registro_valido_redirige_al_acceso()
    {
        using var ctx = new BunitContext();
        var registrar = new Mock<ICommandHandler<RegistrarSolicitanteCommand, Guid>>();
        registrar.Setup(x => x.HandleAsync(
                It.IsAny<RegistrarSolicitanteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));
        ctx.Services.AddSingleton(registrar.Object);

        var cut = ctx.Render<Registro>();
        cut.Find("#nombre").Change("Ana");
        cut.Find("#email").Change("ana@example.com");
        cut.Find("#contrasena").Change("Clave1234");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => ctx.Services.GetRequiredService<NavigationManager>()
            .Uri.Should().EndWith("/acceso/login"));
    }

    private static BunitContext CrearContexto(Rol? rol)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IEvaluadorPermisos>(new EvaluadorPermisos());
        ctx.Services.AddSingleton<IUsuarioActual>(new UsuarioDePrueba(rol));
        return ctx;
    }

    private sealed record UsuarioDePrueba(Rol? RolSesion) : IUsuarioActual
    {
        public Rol Rol => RolSesion ?? Domain.Usuarios.Rol.Solicitante;
        public Guid? UsuarioId => RolSesion is null ? null : Guid.NewGuid();
    }
}
