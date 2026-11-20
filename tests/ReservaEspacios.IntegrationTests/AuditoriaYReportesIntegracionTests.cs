using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Persistence;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// AUD-001/002 y REP-001/002/003 ejecutados sobre el host real y PostgreSQL local.
/// La auditoría se comprueba también en la base de datos: el trigger rechaza UPDATE/DELETE.
/// </summary>
public sealed class AuditoriaYReportesIntegracionTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_AUD_001_Aprobacion_y_cambio_de_rol_registran_actor_nombre_y_rol()
    {
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var coordinadorId = await CrearUsuarioAsync(factory, Rol.Coordinador);
        var adminId = await CrearUsuarioAsync(factory, Rol.Administrador);
        var objetivoId = await CrearUsuarioAsync(factory, Rol.Solicitante);
        using var coordinador = CrearFactory(Rol.Coordinador, coordinadorId);
        var espacioId = await SembrarEspacioAsync("Auditoría aprobación");
        var reservaId = await SembrarReservaAsync(espacioId, Guid.NewGuid());
        (await coordinador.CreateClient().PostAsJsonAsync(
            $"/api/reservas/{reservaId}/aprobacion", new { })).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // Se resuelve por DI para que el caso de uso reciba el IDomainEventDispatcher
        // real; si se construye a mano, el parámetro opcional `eventos` queda nulo y
        // no se publica RolCambiado.
        using var administrador = CrearFactory(Rol.Administrador, adminId);
        using (var scope = administrador.Services.CreateScope())
        {
            var cambiar = scope.ServiceProvider.GetRequiredService<CambiarRolUsuario>();
            (await cambiar.HandleAsync(
                new CambiarRolUsuarioCommand(objetivoId, Rol.Coordinador))).IsSuccess
                .Should().BeTrue();
        }

        var respuesta = await coordinador.CreateClient().GetAsync("/api/auditoria");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var filas = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var aprobacion = filas.EnumerateArray().Single(x =>
            x.GetProperty("entidadId").GetGuid() == reservaId &&
            x.GetProperty("accion").GetString() == "APROBADA");
        aprobacion.GetProperty("actorId").GetGuid().Should().Be(coordinadorId);
        aprobacion.GetProperty("actorNombre").GetString().Should().NotBeNullOrEmpty();
        aprobacion.GetProperty("actorRol").GetString().Should().Be("Coordinador de espacios");

        var cambioRol = filas.EnumerateArray().Single(x =>
            x.GetProperty("entidadId").GetGuid() == objetivoId &&
            x.GetProperty("accion").GetString() == "ROL_CAMBIADO");
        cambioRol.GetProperty("actorRol").GetString().Should().Be("Administrador del sistema");
        cambioRol.GetProperty("metadata").GetString().Should().Contain("COORDINADOR");
    }

    [Fact]
    public async Task TC_AUD_001_Update_y_delete_sobre_audit_logs_son_rechazados()
    {
        var registroId = Guid.NewGuid();
        await using (var contexto = fixture.CreateContext())
        {
            contexto.AuditLogs.Add(new AuditLog
            {
                Id = registroId,
                Entidad = "Reservation",
                EntidadId = Guid.NewGuid(),
                Accion = "APROBADA",
                OcurridoEn = DateTimeOffset.UtcNow
            });
            await contexto.SaveChangesAsync();
        }

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            foreach (var sql in new[]
                     {
                         $"UPDATE audit_logs SET accion = 'ALTERADA' WHERE id = '{registroId}'",
                         $"DELETE FROM audit_logs WHERE id = '{registroId}'"
                     })
            {
                await using var comando = connection.CreateCommand();
                comando.CommandText = sql;
                var fallo = await Assert.ThrowsAsync<PostgresException>(
                    () => comando.ExecuteNonQueryAsync());
                fallo.Message.Should().Contain("AUDITORIA_INMUTABLE");
            }
        }

        using var factory = CrearFactory(Rol.Administrador, Guid.NewGuid());
        using var scope = factory.Services.CreateScope();
        var eliminar = scope.ServiceProvider.GetRequiredService<EliminarAuditoria>();
        var resultado = await eliminar.HandleAsync(new EliminarAuditoriaCommand(registroId));
        resultado.ErrorCode.Should().Be(DomainErrors.AuditoriaInmutable);
        await using var verificacion = fixture.CreateContext();
        (await verificacion.AuditLogs.CountAsync(a => a.Id == registroId)).Should().Be(1);
    }

    [Fact]
    public async Task TC_AUD_002_Consulta_filtrada_por_entidad_actor_y_rango_de_fechas()
    {
        var actorId = Guid.NewGuid();
        var reservaFiltrada = Guid.NewGuid();
        var otraReserva = Guid.NewGuid();
        await using (var contexto = fixture.CreateContext())
        {
            contexto.AuditLogs.AddRange(
                Registro(actorId, "Reservation", reservaFiltrada, "APROBADA",
                    new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero)),
                Registro(actorId, "Reservation", otraReserva, "CANCELADA",
                    new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
                Registro(Guid.NewGuid(), "User", Guid.NewGuid(), "ROL_CAMBIADO",
                    new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero)));
            await contexto.SaveChangesAsync();
        }

        using var coordinador = CrearFactory(Rol.Coordinador, Guid.NewGuid());
        var url = "/api/auditoria?entidad=Reservation" +
                  $"&actorId={actorId}&desde=2026-11-20T00:00:00Z&hasta=2026-11-21T00:00:00Z";
        var respuesta = await coordinador.CreateClient().GetAsync(url);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var filas = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        filas.GetArrayLength().Should().Be(1);
        filas[0].GetProperty("entidadId").GetGuid().Should().Be(reservaFiltrada);

        using var solicitante = CrearFactory(Rol.Solicitante, Guid.NewGuid());
        (await solicitante.CreateClient().GetAsync("/api/auditoria")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TC_REP_001_Reporte_por_espacio_excluye_canceladas_y_filtra_por_fechas()
    {
        var espacioA = await SembrarEspacioAsync("Laboratorio reporte A");
        var espacioB = await SembrarEspacioAsync("Auditorio reporte B");
        await SembrarReservaConEstadoAsync(espacioA, EstadoReserva.Aprobada,
            new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero));
        await SembrarReservaConEstadoAsync(espacioA, EstadoReserva.Completada,
            new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 19, 0, 0, TimeSpan.Zero));
        await SembrarReservaConEstadoAsync(espacioA, EstadoReserva.Cancelada,
            new DateTimeOffset(2026, 11, 20, 17, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero));
        await SembrarReservaConEstadoAsync(espacioA, EstadoReserva.Aprobada,
            new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 20, 16, 0, 0, TimeSpan.Zero));
        await SembrarReservaConEstadoAsync(espacioB, EstadoReserva.Aprobada,
            new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero));
        using var factory = CrearFactory(Rol.Coordinador, Guid.NewGuid());

        var respuesta = await factory.CreateClient().GetAsync(UrlReporte(espacioA));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var espacios = (await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("espacios");
        espacios.GetArrayLength().Should().Be(1);
        var fila = espacios[0];
        fila.GetProperty("espacioId").GetGuid().Should().Be(espacioA);
        fila.GetProperty("reservas").GetInt32().Should().Be(2);
        fila.GetProperty("horasReservadas").GetDouble().Should().Be(2);
        fila.GetProperty("horasDisponibles").GetDouble().Should().Be(20);
        fila.GetProperty("porcentajeOcupacion").GetDouble().Should().Be(10);
    }

    [Fact]
    public async Task TC_REP_003_Solapes_entre_COMPLETADA_y_APROBADA_no_se_duplican()
    {
        var espacioId = await SembrarEspacioAsync("Laboratorio solapes");
        await SembrarReservaConEstadoAsync(espacioId, EstadoReserva.Completada,
            new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero));
        await SembrarReservaConEstadoAsync(espacioId, EstadoReserva.Aprobada,
            new DateTimeOffset(2026, 11, 20, 15, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 16, 30, 0, TimeSpan.Zero));
        using var factory = CrearFactory(Rol.Administrador, Guid.NewGuid());

        var espacios = (await factory.CreateClient().GetAsync(UrlReporte(espacioId)))
            .Content.ReadFromJsonAsync<JsonElement>();

        (await espacios).GetProperty("espacios")[0]
            .GetProperty("horasReservadas").GetDouble().Should().Be(1.5);
        (await espacios).GetProperty("espacios")[0]
            .GetProperty("porcentajeOcupacion").GetDouble().Should().Be(7.5);
    }

    [Fact]
    public async Task TC_REP_002_Solicitante_recibe_NO_AUTORIZADO_en_el_endpoint()
    {
        using var factory = CrearFactory(Rol.Solicitante, Guid.NewGuid());

        var respuesta = await factory.CreateClient().GetAsync(UrlReporte(Guid.NewGuid()));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await respuesta.Content.ReadFromJsonAsync<ErrorDto>();
        error!.Error.Should().Be(DomainErrors.NoAutorizado);
    }

    private static string UrlReporte(Guid espacioId) =>
        "/api/reportes/ocupacion" +
        $"?spaceId={espacioId}&desde=2026-11-20T00:00:00Z&hasta=2026-11-21T00:00:00Z";

    private WebApplicationFactory<Program> CrearFactory(Rol rol, Guid usuarioId) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            builder.UseSetting("Notificaciones:IntervaloSegundos", "3600");
            builder.UseSetting("Reservas:CierreAutomatico:IntervaloSegundos", "3600");
            builder.ConfigureTestServices(servicios =>
                servicios.AddSingleton<IUsuarioActual>(new UsuarioAutenticado(rol, usuarioId)));
        });

    private async Task<Guid> CrearUsuarioAsync(WebApplicationFactory<Program> factory, Rol rol)
    {
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IUsuarioIdentityService>();
        var resultado = await identity.CrearAsync(new CrearUsuarioCommand(
            $"Usuario {rol} {Guid.NewGuid():N}",
            $"{rol}-{Guid.NewGuid():N}@universidad.edu",
            "Segura123", rol));
        resultado.IsSuccess.Should().BeTrue();
        return resultado.Value;
    }

    private async Task<Guid> SembrarEspacioAsync(string nombre)
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(), $"{nombre} {Guid.NewGuid():N}", EspacioTipo.Laboratorio, 30,
            "Bloque A", new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private async Task<Guid> SembrarReservaAsync(Guid espacioId, Guid solicitanteId)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacioId, solicitanteId,
            new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero), "Clase", 10);
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
        return reserva.Id;
    }

    private async Task SembrarReservaConEstadoAsync(
        Guid espacioId, EstadoReserva estado, DateTimeOffset inicio, DateTimeOffset fin)
    {
        await using var contexto = fixture.CreateContext();
        var reserva = new Reservation(
            Guid.NewGuid(), espacioId, Guid.NewGuid(), inicio, fin, "Clase", 10);
        var actor = ActorTransicion.Usuario(Guid.NewGuid(), Rol.Coordinador);
        switch (estado)
        {
            case EstadoReserva.Aprobada:
                reserva.Aprobar(actor);
                break;
            case EstadoReserva.Completada:
                reserva.Aprobar(actor);
                reserva.Completar(actor);
                break;
            case EstadoReserva.Cancelada:
                reserva.Cancelar(actor, "Reprogramada");
                break;
        }
        contexto.Reservations.Add(reserva);
        await contexto.SaveChangesAsync();
    }

    private static AuditLog Registro(
        Guid actorId, string entidad, Guid entidadId, string accion, DateTimeOffset ocurridoEn) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            OcurridoEn = ocurridoEn
        };

    private sealed record ErrorDto(string Error);

    private sealed record UsuarioAutenticado(Rol Rol, Guid? UsuarioId) : IUsuarioActual;
}
