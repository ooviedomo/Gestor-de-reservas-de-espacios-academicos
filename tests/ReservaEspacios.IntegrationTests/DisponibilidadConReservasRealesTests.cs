using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// Tarea 3.1: el repositorio EF real sustituye al stub. Estas pruebas comprueban que
/// `ConsultarDisponibilidad` ve lo que hay en `reservations`, sin fakes, y que una
/// mutación invalida la caché dentro del mismo scope.
/// </summary>
public sealed class DisponibilidadConReservasRealesTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    // 15:00Z–16:00Z = 10:00–11:00 en America/Bogota, dentro de 08:00–18:00.
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(EstadoReserva.Pendiente, MotivosDisponibilidad.SolapeConReserva)]
    [InlineData(EstadoReserva.Aprobada, MotivosDisponibilidad.SolapeConReserva)]
    [InlineData(EstadoReserva.Rechazada, MotivosDisponibilidad.Ninguno)]
    [InlineData(EstadoReserva.Cancelada, MotivosDisponibilidad.Ninguno)]
    [InlineData(EstadoReserva.Completada, MotivosDisponibilidad.Ninguno)]
    public async Task TC_DISP_002_Repositorio_real_refleja_el_estado_persistido(
        EstadoReserva estado, string motivoEsperado)
    {
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, estado);
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var dto = await client.GetFromJsonAsync<DisponibilidadDto>(Url(espacio, Inicio, Fin));

        dto!.Motivo.Should().Be(motivoEsperado);
        dto.Estado.Should().Be(motivoEsperado == MotivosDisponibilidad.SolapeConReserva
            ? EstadosDisponibilidad.NoDisponible
            : EstadosDisponibilidad.Disponible);
    }

    [Fact]
    public async Task TC_DISP_002_Reserva_contigua_no_bloquea_con_datos_reales()
    {
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, EstadoReserva.Aprobada);
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var dto = await client.GetFromJsonAsync<DisponibilidadDto>(Url(espacio, Fin, Fin.AddHours(1)));

        dto!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
        dto.Motivo.Should().Be(MotivosDisponibilidad.Ninguno);
    }

    [Fact]
    public async Task TC_DISP_002_Crear_reserva_invalida_la_cache_de_disponibilidad()
    {
        var espacio = await SembrarEspacioAsync();
        using var factory = CrearFactory();
        using var scope = factory.Services.CreateScope();
        var consultar = scope.ServiceProvider.GetRequiredService<IConsultarDisponibilidad>();
        var crear = scope.ServiceProvider.GetRequiredService<CrearReserva>();

        var antes = await consultar.HandleAsync(
            new ConsultarDisponibilidadCommand(espacio, Inicio, Fin));
        antes.Value!.Estado.Should().Be(EstadosDisponibilidad.Disponible);

        var creacion = await crear.HandleAsync(
            new CrearReservaCommand(espacio, Inicio, Fin, "Clase de laboratorio", 20));
        creacion.IsSuccess.Should().BeTrue();

        var despues = await consultar.HandleAsync(
            new ConsultarDisponibilidadCommand(espacio, Inicio, Fin));
        despues.Value!.Estado.Should().Be(EstadosDisponibilidad.NoDisponible);
        despues.Value.Motivo.Should().Be(MotivosDisponibilidad.SolapeConReserva);
    }

    private WebApplicationFactory<Program> CrearFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(new UsuarioSolicitante(Guid.NewGuid())));
        });

    private async Task<Guid> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var sufijo = Guid.NewGuid().ToString("N")[..6];
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio A-101-{sufijo}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task SembrarReservaAsync(Guid espacio, EstadoReserva estado)
    {
        await using var contexto = fixture.CreateContext();
        contexto.Reservations.Add(ReservaEnEstado(espacio, estado));
        await contexto.SaveChangesAsync();
    }

    private static Reservation ReservaEnEstado(Guid espacio, EstadoReserva estado)
    {
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase existente", 10);
        var coordinador = ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador);

        switch (estado)
        {
            case EstadoReserva.Pendiente:
                break;
            case EstadoReserva.Aprobada:
                reserva.Aprobar(coordinador);
                break;
            case EstadoReserva.Rechazada:
                reserva.Rechazar(coordinador, "Espacio en mantención");
                break;
            case EstadoReserva.Cancelada:
                reserva.Cancelar(coordinador, "Docencia reprogramada");
                break;
            case EstadoReserva.Completada:
                reserva.Aprobar(coordinador);
                reserva.Completar(coordinador);
                break;
        }

        reserva.Estado.Should().Be(estado);
        return reserva;
    }

    private static string Url(Guid spaceId, DateTimeOffset inicio, DateTimeOffset fin) =>
        $"/api/disponibilidad?spaceId={spaceId}" +
        $"&inicio={inicio.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z" +
        $"&fin={fin.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z";

    private sealed class UsuarioSolicitante(Guid id) : IUsuarioActual
    {
        public Rol Rol => Rol.Solicitante;
        public Guid? UsuarioId => id;
    }
}
