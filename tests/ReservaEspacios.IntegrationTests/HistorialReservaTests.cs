using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Identity;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// RES-004 (tarea 4.3): historial sobre PostgreSQL e Identity reales. Los nombres y
/// etiquetas de rol se resuelven desde usuarios creados en la base de pruebas.
/// </summary>
public sealed class HistorialReservaTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TC_RES_004_Solicitante_ve_solo_sus_reservas()
    {
        using var factory = CrearFactory();
        var ana = await CrearUsuarioAsync(factory, "Ana Pérez", Rol.Solicitante);
        var bruno = await CrearUsuarioAsync(factory, "Bruno Ruiz", Rol.Solicitante);
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, ana.Id, Inicio);
        await SembrarReservaAsync(espacio, bruno.Id, Inicio.AddHours(1));
        using var clienteFactory = CrearFactory(usuarioId: ana.Id);
        var client = clienteFactory.CreateClient();

        var filas = await ObtenerHistorialAsync(client, $"?spaceId={espacio.Id}");

        filas.Should().ContainSingle();
        filas[0].Solicitante.Should().Be("Ana Pérez");
    }

    [Fact]
    public async Task TC_RES_004_Coordinador_ve_todas_las_reservas()
    {
        using var factory = CrearFactory();
        var ana = await CrearUsuarioAsync(factory, "Ana Pérez", Rol.Solicitante);
        var bruno = await CrearUsuarioAsync(factory, "Bruno Ruiz", Rol.Solicitante);
        var coordinador = await CrearUsuarioAsync(factory, "Laura Gómez", Rol.Coordinador);
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, ana.Id, Inicio);
        await SembrarReservaAsync(espacio, bruno.Id, Inicio.AddHours(1));
        using var clienteFactory = CrearFactory(usuarioId: coordinador.Id, rol: Rol.Coordinador);
        var client = clienteFactory.CreateClient();

        var filas = await ObtenerHistorialAsync(client, $"?spaceId={espacio.Id}");

        filas.Should().HaveCount(2);
        filas.Select(x => x.Solicitante).Should()
            .BeEquivalentTo(["Ana Pérez", "Bruno Ruiz"]);
    }

    [Fact]
    public async Task TC_RES_004_Filtro_por_estado()
    {
        using var factory = CrearFactory();
        var ana = await CrearUsuarioAsync(factory, "Ana Pérez", Rol.Solicitante);
        var coordinador = await CrearUsuarioAsync(factory, "Laura Gómez", Rol.Coordinador);
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, ana.Id, Inicio, EstadoReserva.Aprobada);
        await SembrarReservaAsync(espacio, ana.Id, Inicio.AddHours(1), EstadoReserva.Cancelada);
        using var clienteFactory = CrearFactory(usuarioId: coordinador.Id, rol: Rol.Coordinador);
        var client = clienteFactory.CreateClient();

        var filas = await ObtenerHistorialAsync(client, $"?estado=CANCELADA&spaceId={espacio.Id}");

        filas.Should().ContainSingle();
        filas[0].Estado.Should().Be("CANCELADA");
    }

    [Fact]
    public async Task TC_RES_004_Decision_registrada_muestra_actor_nombre_y_rol()
    {
        using var factory = CrearFactory();
        var ana = await CrearUsuarioAsync(factory, "Ana Pérez", Rol.Solicitante);
        var coordinador = await CrearUsuarioAsync(factory, "Laura Gómez", Rol.Coordinador);
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(espacio, ana.Id, Inicio, EstadoReserva.Aprobada, coordinador.Id);
        using var clienteFactory = CrearFactory(usuarioId: coordinador.Id, rol: Rol.Coordinador);
        var client = clienteFactory.CreateClient();

        var filas = await ObtenerHistorialAsync(client, $"?spaceId={espacio.Id}");

        var fila = filas.Should().ContainSingle().Subject;
        fila.Actor.Should().Be("Laura Gómez");
        fila.RolActor.Should().Be("Coordinador de espacios");
        fila.AprobadoEn.Should().NotBeNull();
        fila.Espacio.Should().Be(espacio.Nombre);
    }

    [Fact]
    public async Task TC_RES_004_Motivo_de_rechazo_y_rol_del_administrador()
    {
        using var factory = CrearFactory();
        var ana = await CrearUsuarioAsync(factory, "Ana Pérez", Rol.Solicitante);
        var administrador = await CrearUsuarioAsync(factory, "Carlos Díaz", Rol.Administrador);
        var espacio = await SembrarEspacioAsync();
        await SembrarReservaAsync(
            espacio, ana.Id, Inicio, EstadoReserva.Rechazada, administrador.Id,
            "Espacio en mantención");
        using var clienteFactory = CrearFactory(usuarioId: ana.Id);
        var client = clienteFactory.CreateClient();

        var filas = await ObtenerHistorialAsync(client, $"?spaceId={espacio.Id}");

        var fila = filas.Should().ContainSingle().Subject;
        fila.MotivoRechazo.Should().Be("Espacio en mantención");
        fila.Actor.Should().Be("Carlos Díaz");
        fila.RolActor.Should().Be("Administrador del sistema");
    }

    private WebApplicationFactory<Program> CrearFactory(Guid? usuarioId = null, Rol rol = Rol.Solicitante) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            if (usuarioId is { } id)
            {
                builder.ConfigureTestServices(servicios =>
                    servicios.AddSingleton<IUsuarioActual>(
                        new UsuarioAutenticado(rol, id)));
            }
        });

    private static async Task<ApplicationUser> CrearUsuarioAsync(
        WebApplicationFactory<Program> factory, string nombre, Rol rol)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"hist-{Guid.NewGuid():N}@universidad.edu";
        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = nombre,
            Activo = true
        };

        var creacion = await userManager.CreateAsync(usuario, "Segura123");
        creacion.Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(usuario, rol.Codigo())).Succeeded.Should().BeTrue();
        return usuario;
    }

    private async Task<Space> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio historial {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio;
    }

    private async Task SembrarReservaAsync(
        Space espacio,
        Guid solicitanteId,
        DateTimeOffset inicio,
        EstadoReserva estado = EstadoReserva.Pendiente,
        Guid? actorId = null,
        string? motivo = null)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacio.Id, solicitanteId, inicio, inicio.AddHours(1),
            "Clase existente", 10);
        var actor = ActorTransicion.Usuario(actorId ?? Guid.NewGuid(), Rol.Coordinador);

        switch (estado)
        {
            case EstadoReserva.Aprobada:
                reserva.Aprobar(actor);
                break;
            case EstadoReserva.Cancelada:
                reserva.Cancelar(actor, motivo ?? "Docencia reprogramada");
                break;
            case EstadoReserva.Rechazada:
                reserva.Rechazar(actor, motivo ?? "Espacio en mantención");
                break;
        }

        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
    }

    private static async Task<List<HistorialFila>> ObtenerHistorialAsync(
        HttpClient client, string query = "")
    {
        var respuesta = await client.GetAsync($"/api/reservas/historial{query}");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await respuesta.Content.ReadFromJsonAsync<List<HistorialFila>>(Json))!;
    }

    private sealed record HistorialFila(
        Guid Id,
        string Estado,
        string Espacio,
        string Solicitante,
        string? Actor,
        string? RolActor,
        string? MotivoRechazo,
        string? MotivoCancelacion,
        DateTimeOffset? AprobadoEn);

    private sealed class UsuarioAutenticado(Rol rol, Guid id) : IUsuarioActual
    {
        public Rol Rol => rol;
        public Guid? UsuarioId => id;
    }
}
