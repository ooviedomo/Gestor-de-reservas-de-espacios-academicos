using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// RES-003/AUTH-002 (tarea 3.4): cancelación sobre PostgreSQL local con el host real.
/// Comprueba motivo, actor, transición y que la franja vuelve a estar disponible.
/// </summary>
public sealed class CancelarReservaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_003_Solicitante_cancela_la_suya_y_la_franja_vuelve_a_estar_disponible()
    {
        var espacio = await SembrarEspacioAsync();
        var solicitanteId = Guid.NewGuid();
        var reservaId = await SembrarReservaAsync(espacio, solicitanteId, EstadoReserva.Aprobada);
        using var factory = CrearFactory(Rol.Solicitante, solicitanteId);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/cancelacion", new { motivo = "Docencia reprogramada" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using (var contexto = fixture.CreateContext())
        {
            var reserva = await contexto.Reservations.SingleAsync(r => r.Id == reservaId);
            reserva.Estado.Should().Be(EstadoReserva.Cancelada);
            reserva.MotivoCancelacion.Should().Be("Docencia reprogramada");
            reserva.ActorId.Should().Be(solicitanteId);
            reserva.CanceladoEn.Should().NotBeNull();
        }

        var disponibilidad = await client.GetFromJsonAsync<DisponibilidadDto>(Url(espacio));
        disponibilidad!.Estado.Should().Be(EstadosDisponibilidad.Disponible);
        disponibilidad.Motivo.Should().Be(MotivosDisponibilidad.Ninguno);
    }

    [Fact]
    public async Task TC_RES_003_Solicitante_no_cancela_la_de_otro_devuelve_NO_AUTORIZADO()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaAsync(espacio, Guid.NewGuid());
        using var factory = CrearFactory(Rol.Solicitante, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/cancelacion", new { motivo = "Docencia reprogramada" });

        await DevolverError(respuesta, HttpStatusCode.Forbidden, DomainErrors.NoAutorizado);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task TC_RES_003_y_AUTH_002_Coordinador_cancela_reserva_ajena()
    {
        var espacio = await SembrarEspacioAsync();
        var coordinadorId = Guid.NewGuid();
        var reservaId = await SembrarReservaAsync(espacio, Guid.NewGuid(), EstadoReserva.Aprobada);
        using var factory = CrearFactory(Rol.Coordinador, coordinadorId);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/cancelacion",
            new { motivo = "Espacio fuera de servicio" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.Id == reservaId);
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.ActorId.Should().Be(coordinadorId);
    }

    [Fact]
    public async Task TC_RES_003_Cancelacion_sin_motivo_devuelve_MOTIVO_OBLIGATORIO()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaAsync(espacio, Guid.NewGuid());
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/cancelacion", new { motivo = "   " });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.MotivoObligatorio);
        await using var contexto = fixture.CreateContext();
        (await contexto.Reservations.SingleAsync(r => r.Id == reservaId)).Estado
            .Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task TC_RES_003_Estado_terminal_devuelve_TRANSICION_INVALIDA()
    {
        var espacio = await SembrarEspacioAsync();
        var reservaId = await SembrarReservaAsync(espacio, Guid.NewGuid(), EstadoReserva.Rechazada);
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{reservaId}/cancelacion", new { motivo = "Motivo" });

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.TransicionInvalida);
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
            $"Laboratorio cancelar {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task<Guid> SembrarReservaAsync(
        Guid espacio, Guid usuarioId, EstadoReserva estado = EstadoReserva.Pendiente)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio, usuarioId, Inicio, Fin, "Clase existente", 10);
        var coordinador = ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador);

        switch (estado)
        {
            case EstadoReserva.Aprobada:
                reserva.Aprobar(coordinador);
                break;
            case EstadoReserva.Rechazada:
                reserva.Rechazar(coordinador, "Espacio en mantención");
                break;
        }

        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private static string Url(Guid espacio) =>
        $"/api/disponibilidad?spaceId={espacio}" +
        $"&inicio={Inicio.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z" +
        $"&fin={Fin.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z";

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
