using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// UI-002/UI-004: verifica el flujo real sobre el host, incluida la cookie que emite
/// Identity al autenticar y su eliminación al cerrar la sesión.
/// </summary>
public sealed class FlujoAccesoWebTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_UI_002_004_Login_cookie_portada_autenticada_y_logout()
    {
        using var factory = CrearFactory();
        using var client = CrearCliente(factory);

        var email = $"ui-{Guid.NewGuid():N}@example.com";
        var contrasena = "Clave1234";
        var registro = await client.PostAsJsonAsync("/api/autenticacion/registro",
            new { nombre = "Ana", email, contrasena });
        registro.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/autenticacion/login",
            new { email, contrasena });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        login.Headers.Should().Contain(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        var autenticada = await client.GetStringAsync("/");
        autenticada.Should().Contain("Solicitar reserva");
        autenticada.Should().Contain("Cerrar sesión");
        autenticada.Should().Contain("Solicitante");
        autenticada.Should().NotContain("/acceso/login");

        var logout = await client.PostAsync("/api/autenticacion/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var anonima = await client.GetStringAsync("/");
        anonima.Should().Contain("/acceso/login");
        anonima.Should().Contain("/acceso/registro");
        anonima.Should().NotContain("Solicitar reserva");
        anonima.Should().NotContain("Cerrar sesión");
    }

    [Fact]
    public async Task TC_UI_002_Login_formulario_emite_cookie_y_muestra_el_menu_del_rol()
    {
        using var factory = CrearFactory();
        using var client = CrearCliente(factory);
        var (_, email, contrasena) = await RegistrarSolicitanteAsync(client);

        var acceso = await client.PostAsync("/acceso/autenticar", Formulario(email, contrasena));

        acceso.StatusCode.Should().Be(HttpStatusCode.Found);
        acceso.Headers.Location!.ToString().Should().Be("/");
        acceso.Headers.Should().Contain(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        var portada = await client.GetStringAsync("/");
        portada.Should().Contain("Solicitar reserva");
        portada.Should().Contain("Cerrar sesión");
        portada.Should().Contain("Solicitante");
        portada.Should().NotContain("/acceso/login");
    }

    [Fact]
    public async Task TC_UI_002_Login_formulario_credenciales_invalidas_no_emite_cookie()
    {
        using var factory = CrearFactory();
        using var client = CrearCliente(factory);
        var (_, email, _) = await RegistrarSolicitanteAsync(client);

        var acceso = await client.PostAsync("/acceso/autenticar",
            Formulario(email, "ContrasenaIncorrecta1"));

        acceso.StatusCode.Should().Be(HttpStatusCode.Found);
        acceso.Headers.Location!.ToString().Should().Be("/acceso/login?error=CREDENCIALES_INVALIDAS");
        acceso.Headers.Should().NotContain(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        var formulario = await client.GetStringAsync(acceso.Headers.Location);
        formulario.Should().Contain("CREDENCIALES_INVALIDAS");
    }

    [Fact]
    public async Task TC_UI_002_Login_formulario_cuenta_desactivada_no_emite_cookie()
    {
        using var factory = CrearFactory();
        using var client = CrearCliente(factory);
        var (id, email, contrasena) = await RegistrarSolicitanteAsync(client);

        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IUsuarioIdentityService>();
        var desactivacion = await identity.ActivarDesactivarAsync(id, false);
        desactivacion.IsSuccess.Should().BeTrue();

        var acceso = await client.PostAsync("/acceso/autenticar", Formulario(email, contrasena));

        acceso.StatusCode.Should().Be(HttpStatusCode.Found);
        acceso.Headers.Location!.ToString().Should().Be("/acceso/login?error=CUENTA_DESACTIVADA");
        acceso.Headers.Should().NotContain(x => x.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
    }

    private WebApplicationFactory<Program> CrearFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString));

    private static HttpClient CrearCliente(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static FormUrlEncodedContent Formulario(string email, string contrasena) =>
        new(new Dictionary<string, string>
        {
            ["email"] = email,
            ["contrasena"] = contrasena
        });

    private static async Task<(Guid Id, string Email, string Contrasena)> RegistrarSolicitanteAsync(
        HttpClient client)
    {
        var email = $"ui-form-{Guid.NewGuid():N}@example.com";
        const string contrasena = "Clave1234";
        var respuesta = await client.PostAsJsonAsync("/api/autenticacion/registro",
            new { nombre = "Ana", email, contrasena });
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (cuerpo.GetProperty("id").GetGuid(), email, contrasena);
    }
}
