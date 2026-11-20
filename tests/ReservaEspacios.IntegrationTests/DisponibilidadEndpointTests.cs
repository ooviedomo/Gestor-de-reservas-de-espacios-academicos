using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Infrastructure.Persistence;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>DISP-001/002/003 expuestas por HTTP y verificadas con WebApplicationFactory.</summary>
public sealed class DisponibilidadEndpointTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_DISP_001_Endpoint_devuelve_DISPONIBLE()
    {
        using var factory = CrearFactory();
        var (activo, _) = await SembrarAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync(Url(activo, "2026-11-20T15:00:00Z", "2026-11-20T16:00:00Z"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DisponibilidadDto>();
        dto!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
        dto.Motivo.Should().Be(MotivosDisponibilidad.Ninguno);
    }

    [Fact]
    public async Task TC_DISP_001_Endpoint_devuelve_ESPACIO_INACTIVO()
    {
        using var factory = CrearFactory();
        var (_, inactivo) = await SembrarAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync(Url(inactivo, "2026-11-20T15:00:00Z", "2026-11-20T16:00:00Z"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DisponibilidadDto>();
        dto!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        dto.Motivo.Should().Be(MotivosDisponibilidad.EspacioInactivo);
    }

    [Fact]
    public async Task TC_DISP_001_Endpoint_devuelve_FUERA_DE_HORARIO()
    {
        using var factory = CrearFactory();
        var (activo, _) = await SembrarAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync(Url(activo, "2026-11-21T00:00:00Z", "2026-11-21T01:00:00Z"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DisponibilidadDto>();
        dto!.Motivo.Should().Be(MotivosDisponibilidad.FueraDeHorario);
    }

    [Fact]
    public async Task TC_DISP_002_Endpoint_devuelve_SOLAPE_CON_RESERVA()
    {
        var reservas = new ReservasFake(
            new ReservaBloqueante(
                Guid.NewGuid(), Guid.Empty, "APROBADA",
                new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero)));
        using var factory = CrearFactory(reservas);
        var (activo, _) = await SembrarAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync(Url(activo, "2026-11-20T15:30:00Z", "2026-11-20T16:30:00Z"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DisponibilidadDto>();
        dto!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        dto.Motivo.Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    private WebApplicationFactory<Program> CrearFactory(ReservasFake? reservas = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            if (reservas is not null)
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IDisponibilidadRepository>();
                    services.AddSingleton<IDisponibilidadRepository>(reservas);
                });
            }
        });

    private async Task<(Guid Activo, Guid Inactivo)> SembrarAsync()
    {
        await using var context = fixture.CreateContext();
        var sufijo = Guid.NewGuid().ToString("N")[..6];
        var activo = Crear($"Laboratorio A-101-{sufijo}", true);
        var inactivo = Crear($"Auditorio Central-{sufijo}", false);
        context.Spaces.AddRange(activo, inactivo);
        await context.SaveChangesAsync();
        return (activo.Id, inactivo.Id);
    }

    private static Space Crear(string nombre, bool activo) => new(
        Guid.NewGuid(),
        nombre,
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)),
        activo);

    private static string Url(Guid spaceId, string inicio, string fin) =>
        $"/api/disponibilidad?spaceId={spaceId}&inicio={inicio}&fin={fin}";

    private sealed class ReservasFake(params ReservaBloqueante[] reservas) : IDisponibilidadRepository
    {
        public Task<IReadOnlyList<ReservaBloqueante>> ObtenerReservasAsync(
            Guid spaceId,
            DateTimeOffset inicio,
            DateTimeOffset fin,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReservaBloqueante>>(reservas);
    }
}
