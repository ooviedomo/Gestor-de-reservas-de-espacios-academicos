using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Tarea 4.4: las páginas de aprobación e historial se sirven desde el host real y
/// el endpoint que las alimenta respeta la visibilidad por rol (resumen de RES-004).
/// </summary>
public sealed class PaginasReservasTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_004_Coordinador_y_solicitante_reciben_historiales_distintos()
    {
        var espacio = await SembrarEspacioAsync();
        var anaId = Guid.NewGuid();
        var brunoId = Guid.NewGuid();
        await SembrarReservaAsync(espacio, anaId, Inicio);
        await SembrarReservaAsync(espacio, brunoId, Inicio.AddHours(1));

        using var factoryAna = CrearFactory(Rol.Solicitante, anaId);
        using var factoryCoordinador = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var query = $"?spaceId={espacio.Id}";

        var filasAna = await ObtenerHistorialAsync(factoryAna, query);
        var filasCoordinador = await ObtenerHistorialAsync(factoryCoordinador, query);

        filasAna.Should().ContainSingle();
        filasCoordinador.Should().HaveCount(2);
        filasCoordinador.Select(x => x.Id).Should().Contain(filasAna[0].Id);
    }

    [Theory]
    [InlineData("/reservas/aprobaciones", "Panel de aprobación")]
    [InlineData("/reservas/historial", "Historial de reservas")]
    public async Task TC_RES_004_Paginas_de_aprobacion_e_historial_responden_200(
        string ruta, string titulo)
    {
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain(titulo);
    }

    private WebApplicationFactory<Program> CrearFactory(Rol rol, Guid usuarioId) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(
                    new UsuarioAutenticado(rol, usuarioId)));
        });

    private async Task<Space> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio páginas {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio;
    }

    private async Task SembrarReservaAsync(Space espacio, Guid solicitanteId, DateTimeOffset inicio)
    {
        await using var contexto = fixture.CreateContext();
        contexto.Reservations.Add(new Reservation(
            Guid.NewGuid(), espacio.Id, solicitanteId, inicio, inicio.AddHours(1),
            "Clase existente", 10));
        await contexto.SaveChangesAsync();
    }

    private static async Task<List<HistorialFila>> ObtenerHistorialAsync(
        WebApplicationFactory<Program> factory, string query)
    {
        var client = factory.CreateClient();
        var respuesta = await client.GetAsync($"/api/reservas/historial{query}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await respuesta.Content.ReadFromJsonAsync<List<HistorialFila>>(Json))!;
    }

    private sealed record HistorialFila(Guid Id, string Solicitante);

    private sealed class UsuarioAutenticado(Rol rol, Guid id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
