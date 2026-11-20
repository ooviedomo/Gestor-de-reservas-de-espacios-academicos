using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Identity;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>Tarea 1.1: la migración siembra exactamente tres roles con etiqueta en español.</summary>
public sealed class IdentityRolesTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_USR_ROLES_001_La_migracion_inserta_exactamente_tres_roles()
    {
        await using var context = fixture.CreateContext();

        var roles = await context.Roles.AsNoTracking().ToListAsync();

        roles.Should().HaveCount(3);
        roles.Select(x => x.Name).Should().BeEquivalentTo(
            Rol.Solicitante.Codigo(), Rol.Coordinador.Codigo(), Rol.Administrador.Codigo());
        roles.Single(x => x.Name == Rol.Solicitante.Codigo()).Etiqueta
            .Should().Be("Solicitante");
        roles.Single(x => x.Name == Rol.Coordinador.Codigo()).Etiqueta
            .Should().Be("Coordinador de espacios");
        roles.Single(x => x.Name == Rol.Administrador.Codigo()).Etiqueta
            .Should().Be("Administrador del sistema");
    }

    [Fact]
    public async Task TC_USR_ROLES_002_El_email_se_configura_como_nombre_de_usuario_unico()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString));
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        options.User.RequireUniqueEmail.Should().BeTrue();
        options.Password.RequiredLength.Should().Be(8);
        options.Password.RequireDigit.Should().BeTrue();
        options.Password.RequireUppercase.Should().BeTrue();
    }
}
