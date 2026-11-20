using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Tarea 2.3: verificación extremo a extremo. El cliente HTTP recibe el código
/// `SOLAPE_DETECTADO` con estado 409, nunca el error SQL de PostgreSQL.
/// </summary>
public sealed class SolapeEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_002_Cliente_recibe_409_SOLAPE_DETECTADO_sin_SQL_crudo()
    {
        var espacio = Guid.NewGuid();
        await using (var contexto = fixture.CreateContext())
        {
            // La cadena de RES-001 exige un espacio activo con horario válido.
            contexto.Spaces.Add(new Space(
                espacio,
                $"Laboratorio solape {Guid.NewGuid():N}",
                EspacioTipo.Laboratorio,
                30,
                "Bloque A",
                new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0))));
            contexto.Reservations.Add(new Reservation(
                Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase existente", 10));
            await contexto.SaveChangesAsync();
        }

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(new UsuarioSolicitante(Guid.NewGuid())));
        });
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/reservas", new
        {
            spaceId = espacio,
            inicio = Inicio.AddMinutes(30),
            fin = Fin.AddMinutes(30),
            proposito = "Otra clase",
            asistentes = 10
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var error = await respuesta.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Error.Should().Be(DomainErrors.SolapeDetectado);
        cuerpo.Should().NotContain("23P01");
        cuerpo.Should().NotContain("no_overlap");
        cuerpo.Should().NotContain("EXCLUDE");
    }

    [Fact]
    public async Task TC_RES_002_Cliente_recibe_400_RANGO_INVALIDO()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(new UsuarioSolicitante(Guid.NewGuid())));
        });
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/reservas", new
        {
            spaceId = Guid.NewGuid(),
            inicio = Fin,
            fin = Inicio,
            proposito = "Rango invertido",
            asistentes = 10
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await respuesta.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Error.Should().Be(DomainErrors.RangoInvalido);
    }

    private sealed record ErrorDto(string Error);

    private sealed class UsuarioSolicitante(Guid id) : IUsuarioActual
    {
        public Rol Rol => Rol.Solicitante;
        public Guid? UsuarioId => id;
    }
}
