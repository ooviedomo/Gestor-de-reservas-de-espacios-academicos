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
/// Tareas 3.2/3.3: creación validada con revalidación SERIALIZABLE sobre PostgreSQL
/// local (ADR-003). La cadena RES-001 se ejecuta de extremo a extremo por HTTP.
/// </summary>
public sealed class CrearReservaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    // 15:00Z–16:00Z = 10:00–11:00 en America/Bogota, dentro de 08:00–18:00.
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin =
        new(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_001_Solicitud_valida_crea_reserva_en_estado_PENDIENTE()
    {
        var espacio = await SembrarEspacioAsync();
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/reservas", Comando(espacio));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        await using var contexto = fixture.CreateContext();
        var reserva = await contexto.Reservations.SingleAsync(r => r.SpaceId == espacio);
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        reserva.Inicio.Should().Be(Inicio);
        reserva.Fin.Should().Be(Fin);
        reserva.Proposito.Should().Be("Clase de laboratorio");
        reserva.Asistentes.Should().Be(20);
        reserva.UsuarioId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TC_RES_001_Asistentes_exceden_capacidad_devuelve_CAPACIDAD_EXCEDIDA()
    {
        var espacio = await SembrarEspacioAsync(capacidad: 30);
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            "/api/reservas", Comando(espacio, Inicio, Fin, asistentes: 35));

        await DevolverError(respuesta, HttpStatusCode.BadRequest, DomainErrors.CapacidadExcedida);
    }

    [Fact]
    public async Task TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_devuelve_SOLAPE_DETECTADO()
    {
        var espacio = await SembrarEspacioAsync();
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var previa = await client.GetFromJsonAsync<DisponibilidadDto>(Url(espacio));
        previa!.Estado.Should().Be(EstadosDisponibilidad.Disponible);

        // Otra reserva ocupa la franja después de la consulta informativa.
        await using (var contexto = fixture.CreateContext())
        {
            contexto.Reservations.Add(new Reservation(
                Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase existente", 10));
            await contexto.SaveChangesAsync();
        }

        var respuesta = await client.PostAsJsonAsync("/api/reservas", Comando(espacio));

        await DevolverError(respuesta, HttpStatusCode.Conflict, DomainErrors.SolapeDetectado);
        await using var verificacion = fixture.CreateContext();
        (await verificacion.Reservations.CountAsync(r => r.SpaceId == espacio)).Should().Be(1);
    }

    [Fact]
    public async Task TC_RES_002_Reservas_contiguas_son_aceptadas()
    {
        var espacio = await SembrarEspacioAsync();
        await using (var contexto = fixture.CreateContext())
        {
            contexto.Reservations.Add(new Reservation(
                Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase previa", 10));
            await contexto.SaveChangesAsync();
        }
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            "/api/reservas", Comando(espacio, Fin, Fin.AddHours(1)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task TC_RES_002_Reserva_cancelada_no_bloquea_la_nueva()
    {
        var espacio = await SembrarEspacioAsync();
        await using (var contexto = fixture.CreateContext())
        {
            var cancelada = new Reservation(
                Guid.NewGuid(), espacio, Guid.NewGuid(), Inicio, Fin, "Clase cancelada", 10);
            cancelada.Cancelar(
                Domain.Reservas.ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador),
                "Docencia reprogramada");
            contexto.Reservations.Add(cancelada);
            await contexto.SaveChangesAsync();
        }
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync("/api/reservas", Comando(espacio));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private WebApplicationFactory<Program> CrearFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(new UsuarioSolicitante(Guid.NewGuid())));
        });

    private async Task<Guid> SembrarEspacioAsync(int capacidad = 30)
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio crear {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            capacidad,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private static object Comando(
        Guid espacio,
        DateTimeOffset inicio = default,
        DateTimeOffset fin = default,
        int asistentes = 20) => new
        {
            spaceId = espacio,
            inicio = (inicio == default ? Inicio : inicio).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss") + "Z",
            fin = (fin == default ? Fin : fin).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss") + "Z",
            proposito = "Clase de laboratorio",
            asistentes
        };

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

    private sealed class UsuarioSolicitante(Guid id) : IUsuarioActual
    {
        public Rol Rol => Rol.Solicitante;
        public Guid? UsuarioId => id;
    }
}
