using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Usuarios;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Web.Security;

namespace ReservaEspacios.IntegrationTests;

/// <summary>
/// AUTH-001/002/003 ejecutados sobre el host real con WebApplicationFactory.
/// La autenticación en sí se verifica en TC-USR-003-*; aquí se inyecta el usuario
/// autenticado para que las pruebas se centren en la matriz de permisos y sus guards.
/// Las respuestas 200 confirman que el guard otorgó el permiso para auditoría y
/// reportes, implementados en este change; el solicitante recibe 403.
/// </summary>
public sealed class AutorizacionTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public void TC_AUTH_001_Adapter_traduce_claims_de_Identity_a_rol_y_usuario_del_dominio()
    {
        var usuarioId = Guid.NewGuid();
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Role, Rol.Coordinador.Codigo())
        ], "Identity.Application");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identidad)
            }
        };
        IUsuarioActual adapter = new IdentityRoleAdapter(accessor);

        adapter.Rol.Should().Be(Rol.Coordinador);
        adapter.UsuarioId.Should().Be(usuarioId);
        adapter.EstaAutenticado.Should().BeTrue();
    }

    [Fact]
    public async Task TC_AUTH_001_Acceso_anónimo_al_historial_devuelve_NO_AUTENTICADO()
    {
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.GetAsync("/api/reservas/historial");

        await DevolverError(respuesta, HttpStatusCode.Unauthorized, DomainErrors.NoAutenticado);
    }

    [Fact]
    public async Task TC_AUTH_001_Acceso_anonimo_a_permisos_devuelve_NO_AUTENTICADO()
    {
        using var factory = CrearFactory();
        var client = factory.CreateClient();

        var respuesta = await client.GetAsync("/api/autorizacion/permisos");

        await DevolverError(respuesta, HttpStatusCode.Unauthorized, DomainErrors.NoAutenticado);
    }

    [Fact]
    public async Task TC_AUTH_001_Solicitante_no_aprueba_reservas()
    {
        using var factory = CrearFactory(Rol.Solicitante);
        var client = factory.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            $"/api/reservas/{Guid.NewGuid()}/aprobacion", new { });

        await DevolverError(respuesta, HttpStatusCode.Forbidden, DomainErrors.NoAutorizado);
    }

    [Fact]
    public async Task TC_AUTH_002_Solicitante_puede_crear_reserva_y_no_consultar_auditoria()
    {
        using var factory = CrearFactory(Rol.Solicitante);
        var client = factory.CreateClient();
        var espacio = await SembrarEspacioAsync();

        var creacion = await client.PostAsJsonAsync("/api/reservas", ComandoReservaValido(espacio));
        creacion.StatusCode.Should().Be(HttpStatusCode.Created);

        await DevolverError(
            await client.GetAsync("/api/auditoria"),
            HttpStatusCode.Forbidden,
            DomainErrors.NoAutorizado);
        await DevolverError(
            await client.GetAsync("/api/reportes/ocupacion"),
            HttpStatusCode.Forbidden,
            DomainErrors.NoAutorizado);

        var permisos = await client.GetFromJsonAsync<PermisosDto>("/api/autorizacion/permisos");
        permisos!.CrearReserva.Should().BeTrue();
        permisos.CancelarReservaPropia.Should().BeTrue();
        permisos.Aprobar.Should().BeFalse();
        permisos.AdministrarEspacios.Should().BeFalse();
    }

    [Fact]
    public async Task TC_AUTH_003_Coordinador_no_administra_espacios_ni_crea_reservas()
    {
        using var factory = CrearFactory(Rol.Coordinador);
        using (var scope = factory.Services.CreateScope())
        {
            var registrar = scope.ServiceProvider.GetRequiredService<IRegistrarSpace>();
            var espacio = await registrar.HandleAsync(ComandoEspacio());
            espacio.ErrorCode.Should().Be(DomainErrors.NoAutorizado);

            var crear = scope.ServiceProvider.GetRequiredService<CrearUsuarioPrivilegiado>();
            var usuario = await crear.HandleAsync(ComandoUsuario(Rol.Administrador));
            usuario.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        }
        var client = factory.CreateClient();
        await DevolverError(
            await client.PostAsJsonAsync("/api/reservas", new { }),
            HttpStatusCode.Forbidden,
            DomainErrors.NoAutorizado);
    }

    [Fact]
    public async Task TC_AUTH_001_Coordinador_aprueba_y_consulta_auditoria_y_reportes()
    {
        using var factory = CrearFactory(Rol.Coordinador);
        var client = factory.CreateClient();

        await DevolverError(
            await client.PostAsJsonAsync($"/api/reservas/{Guid.NewGuid()}/aprobacion", new { }),
            HttpStatusCode.BadRequest,
            DomainErrors.ReservaNoEncontrada);
        var auditoria = await client.GetAsync("/api/auditoria");
        auditoria.StatusCode.Should().Be(HttpStatusCode.OK);
        // Otras pruebas de la clase siembran datos en el mismo esquema: basta con
        // comprobar que la consulta está operativa y devuelve el historical append-only.
        (await auditoria.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength()
            .Should().BeGreaterThan(0);

        var reporte = await client.GetAsync("/api/reportes/ocupacion");
        reporte.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reporte.Content.ReadFromJsonAsync<JsonElement>()).ValueKind
            .Should().Be(JsonValueKind.Object);

        var permisos = await client.GetFromJsonAsync<PermisosDto>("/api/autorizacion/permisos");
        permisos!.Aprobar.Should().BeTrue();
        permisos.ConsultarAuditoria.Should().BeTrue();
        permisos.ConsultarReportes.Should().BeTrue();
        permisos.AdministrarEspacios.Should().BeFalse();
    }

    [Fact]
    public async Task TC_AUTH_003_Administrador_no_crea_reservas_pero_gestiona_espacios_usuarios_y_aprueba()
    {
        using var factory = CrearFactory(Rol.Administrador);
        using (var scope = factory.Services.CreateScope())
        {
            var registrar = scope.ServiceProvider.GetRequiredService<IRegistrarSpace>();
            var espacio = await registrar.HandleAsync(ComandoEspacio());
            espacio.IsSuccess.Should().BeTrue();

            var crear = scope.ServiceProvider.GetRequiredService<CrearUsuarioPrivilegiado>();
            var usuario = await crear.HandleAsync(ComandoUsuario(Rol.Coordinador));
            usuario.IsSuccess.Should().BeTrue();
        }

        var client = factory.CreateClient();
        await DevolverError(
            await client.PostAsJsonAsync("/api/reservas", new { }),
            HttpStatusCode.Forbidden,
            DomainErrors.NoAutorizado);

        await DevolverError(
            await client.PostAsJsonAsync($"/api/reservas/{Guid.NewGuid()}/aprobacion", new { }),
            HttpStatusCode.BadRequest,
            DomainErrors.ReservaNoEncontrada);

        var permisos = await client.GetFromJsonAsync<PermisosDto>("/api/autorizacion/permisos");
        permisos!.CrearReserva.Should().BeFalse();
        permisos.AdministrarEspacios.Should().BeTrue();
        permisos.AdministrarUsuarios.Should().BeTrue();
        permisos.Aprobar.Should().BeTrue();
    }

    private WebApplicationFactory<Program> CrearFactory(Rol? rol = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:ReservaEspacios", fixture.ConnectionString);
            if (rol is not null)
            {
                builder.ConfigureTestServices(servicios =>
                    servicios.AddSingleton<IUsuarioActual>(new UsuarioAutenticado(rol.Value)));
            }
        });

    private static RegistrarSpaceCommand ComandoEspacio() => new(
        $"Espacio auth {Guid.NewGuid():N}",
        EspacioTipo.Laboratorio,
        20,
        "Bloque A",
        new TimeOnly(8, 0),
        new TimeOnly(18, 0));

    private async Task<Guid> SembrarEspacioAsync()
    {
        await using var contexto = fixture.CreateContext();
        var espacio = new Space(
            Guid.NewGuid(),
            $"Laboratorio auth {Guid.NewGuid():N}",
            EspacioTipo.Laboratorio,
            30,
            "Bloque A",
            new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));
        contexto.Spaces.Add(espacio);
        await contexto.SaveChangesAsync();
        return espacio.Id;
    }

    private static object ComandoReservaValido(Guid spaceId) => new
    {
        spaceId,
        inicio = "2026-11-20T15:00:00Z",
        fin = "2026-11-20T16:00:00Z",
        proposito = "Clase de laboratorio",
        asistentes = 20
    };

    private static CrearUsuarioPrivilegiadoCommand ComandoUsuario(Rol rol) => new(
        "Usuario creado por administrador",
        $"creado-{rol}-{Guid.NewGuid():N}@universidad.edu",
        "Segura123",
        rol);

    private static async Task DevolverError(
        HttpResponseMessage respuesta,
        HttpStatusCode estado,
        string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ErrorDto>();
        cuerpo!.Error.Should().Be(codigo);
    }

    private sealed record ErrorDto(string Error);

    private sealed record PermisosDto(
        bool CrearReserva,
        bool CancelarReservaPropia,
        bool VerHistorialCompleto,
        bool Aprobar,
        bool Completar,
        bool AdministrarEspacios,
        bool AdministrarUsuarios,
        bool ConsultarAuditoria,
        bool ConsultarReportes);

    private sealed class UsuarioAutenticado(Rol rol) : IUsuarioActual
    {
        private readonly Guid _id = Guid.NewGuid();

        public Rol Rol { get; } = rol;
        public Guid? UsuarioId => _id;
    }
}
