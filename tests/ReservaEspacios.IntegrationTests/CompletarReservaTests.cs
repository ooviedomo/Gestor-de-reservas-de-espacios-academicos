using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;
using Xunit.Sdk;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// COM-001 (tarea 4.2): completado manual por HTTP y cierre automático ejecutado por
/// el hosted service sobre PostgreSQL local (ADR-003).
/// </summary>
public sealed class CompletarReservaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset FinVencida =
        new(2020, 11, 20, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FinFutura =
        new(2099, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public async Task TC_COM_001_Rol_con_permiso_completa_y_registra_fecha(Rol rol)
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarAprobadaAsync(espacio, FinVencida);
        using var factory = CrearFactory(rol, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/completacion", new { });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.Id == reservaId);
        reserva.Estado.Should().Be(EstadoReserva.Completada);
        reserva.CompletadoEn.Should().NotBeNull();
    }

    [Fact]
    public async Task TC_COM_001_Solicitante_no_completa_devuelve_NO_AUTORIZADO()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarAprobadaAsync(espacio, FinVencida);
        using var factory = CrearFactory(Rol.Solicitante, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/completacion", new { });

        await DevolverError(respuesta, HttpStatusCode.Forbidden, DomainErrors.NoAutorizado);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Aprobada);
    }

    [Fact]
    public async Task TC_COM_001_Completar_pendiente_devuelve_TRANSICION_INVALIDA()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarPendienteAsync(espacio);
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/completacion", new { });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.TransicionInvalida);
    }

    [Fact]
    public async Task TC_COM_001_Worker_cierra_solo_las_aprobadas_vencidas()
    {
        var espacio = await SembrarEspacioAsync();
        var aprobadorId = Guid.NewGuid();
        var vencidaId = await SembrarAprobadaAsync(espacio, FinVencida, aprobadorId);
        var futuraId = await SembrarAprobadaAsync(espacio, FinFutura, aprobadorId);
        using var factory = CrearFactory(
            Rol.Coordinador, Guid.NewGuid(), intervaloSegundos: 1);
        var client = factory.CreateClient();

        await EsperarAsync(async () =>
        {
            await using var contexto = fixture.CreateContext();
            return (await contexto.Reservations.SingleAsync(r => r.Id == vencidaId)).Estado
                == EstadoReserva.Completada;
        });

        await using var verificacion = fixture.CreateContext();
        var vencida = await verificacion.Reservations.SingleAsync(r => r.Id == vencidaId);
        vencida.CompletadoEn.Should().NotBeNull();
        vencida.AprobadoPor.Should().Be(aprobadorId);
        vencida.ActorId.Should().Be(aprobadorId);
        (await verificacion.Reservations.SingleAsync(r => r.Id == futuraId)).Estado
            .Should().Be(EstadoReserva.Aprobada);
    }

    private WebApplicationFactory<Program> CrearFactory(
        Rol rol, Guid usuarioId, int? intervaloSegundos = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            if (intervaloSegundos is not null)
            {
                builder.UseSetting(
                    "Reservas:CierreAutomatico:IntervaloSegundos",
                    intervaloSegundos.Value.ToString());
            }

            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(
                    new UsuarioAutenticado(rol, usuarioId)));
        });

    private async Task<Guid> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio completar {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task<Guid> SembrarPendienteAsync(Guid espacio)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, Guid.NewGuid(), FinFutura.AddHours(-1), FinFutura,
            "Clase existente", 10);
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private async Task<Guid> SembrarAprobadaAsync(
        Guid espacio, DateTimeOffset fin, Guid? aprobadorId = null)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, Guid.NewGuid(), fin.AddHours(-1), fin,
            "Clase existente", 10);
        reserva.Aprobar(ActorTransicion.Usuario(
            aprobadorId ?? Guid.NewGuid(), Rol.Coordinador));
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private static async Task EsperarAsync(Func<Task<bool>> condicion)
    {
        for (var intento = 0; intento < 50; intento++)
        {
            if (await condicion())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new XunitException("El cierre automático no completó la reserva vencida.");
    }

    private static async Task DevolverError(
        HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        var error = await respuesta.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Error.Should().Be(codigo);
    }

    private sealed record ErrorDto(string Error);

    private sealed class UsuarioAutenticado(Rol rol, Guid id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
