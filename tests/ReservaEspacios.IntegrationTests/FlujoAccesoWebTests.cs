using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

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
        autenticada.Should().NotContain("Acceder o");

        var logout = await client.PostAsync("/api/autenticacion/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var anonima = await client.GetStringAsync("/");
        anonima.Should().Contain("Acceder");
        anonima.Should().Contain("registrarse como solicitante");
        anonima.Should().NotContain("Solicitar reserva");
        anonima.Should().NotContain("Cerrar sesión");
    }
}
