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

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// APR-001 (tarea 4.1): aprobación y rechazo ejecutados sobre PostgreSQL local con
/// el host real. Verifica actor, fecha, motivo, estados y la matriz de permisos.
/// </summary>
public sealed class AprobarReservaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public async Task TC_APR_001_Rol_con_permiso_aprueba_y_registra_actor_y_fecha(Rol rol)
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacio);
        var actorId = Guid.NewGuid();
        using var factory = CrearFactory(rol, actorId);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.Id == reservaId);
        reserva.Estado.Should().Be(EstadoReserva.Aprobada);
        reserva.AprobadoPor.Should().Be(actorId);
        reserva.AprobadoEn.Should().NotBeNull();
        reserva.ActorId.Should().Be(actorId);
    }

    [Fact]
    public async Task TC_APR_001_Administrador_rechaza_con_motivo_y_lo_persiste()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacio);
        var actorId = Guid.NewGuid();
        using var factory = CrearFactory(Rol.Administrador, actorId);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/rechazo",
            new { motivo = "Espacio en mantención" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.Id == reservaId);
        reserva.Estado.Should().Be(EstadoReserva.Rechazada);
        reserva.MotivoRechazo.Should().Be("Espacio en mantención");
        reserva.ActorId.Should().Be(actorId);
    }

    [Fact]
    public async Task TC_APR_001_Rechazo_sin_motivo_devuelve_MOTIVO_OBLIGATORIO()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacio);
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/rechazo", new { motivo = "   " });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.MotivoObligatorio);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task TC_APR_001_Reaprobar_devuelve_TRANSICION_INVALIDA()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaAprobadaAsync(espacio);
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.TransicionInvalida);
    }

    [Fact]
    public async Task TC_APR_001_Solicitante_no_aprueba_devuelve_NO_AUTORIZADO()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaPendienteAsync(espacio);
        using var factory = CrearFactory(Rol.Solicitante, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { });

        await DevolverError(respuesta, HttpStatusCode.Forbidden, DomainErrors.NoAutorizado);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task TC_APR_001_Reserva_inexistente_devuelve_RESERVA_NO_ENCONTRADA()
    {
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{Guid.NewGuid()}/aprobacion", new { });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.ReservaNoEncontrada);
    }

    private WebApplicationFactory<Program> CrearFactory(Rol rol, Guid usuarioId) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(
                    new UsuarioAutenticado(rol, usuarioId)));
        });

    private async Task<Guid> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio aprobación {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task<Guid> SembrarReservaPendienteAsync(Guid espacio)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase existente", 10);
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private async Task<Guid> SembrarReservaAprobadaAsync(Guid espacio)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase existente", 10);
        reserva.Aprobar(ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador));
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
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
