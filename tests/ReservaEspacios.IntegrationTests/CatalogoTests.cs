using FluentAssertions;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Usuarios;
using ReservaEspacios.Infrastructure.Espacios;
using ReservaEspacios.Infrastructure.Persistence;
using Xunit;

namespace ReservaEspacios.IntegrationTests;

/// <summary>ESP-003: catálogo filtrable ejecutado contra PostgreSQL local (ADR-003).</summary>
public sealed class CatalogoTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task TC_ESP_003_Filtro_por_tipo_devuelve_solo_salas_de_computo_activas()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Solicitante));
        var filtro = new FiltroCatalogoBuilder().ConTipo(EspacioTipo.SalaComputo).Build();

        var result = await sut.HandleAsync(filtro);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(x => x.Tipo == EspacioTipo.SalaComputo && x.Activo);
        result.Value.Should().Contain(x => x.Nombre == Nombre("Sala de Cómputo 2", sufijo));
    }

    [Fact]
    public async Task TC_ESP_003_Filtro_por_capacidad_minima()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Solicitante));
        var filtro = new FiltroCatalogoBuilder().ConCapacidadMinima(30).Build();

        var result = await sut.HandleAsync(filtro);

        result.Value.Should().NotBeEmpty();
        result.Value.Should().OnlyContain(x => x.Capacidad >= 30 && x.Activo);
        result.Value.Should().NotContain(x => x.Nombre == Nombre("Sala pequeña", sufijo));
    }

    [Fact]
    public async Task TC_ESP_003_Espacio_desactivado_excluido_por_defecto()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Solicitante));

        var result = await sut.HandleAsync(new FiltroCatalogoBuilder().Build());

        result.Value.Should().Contain(x => x.Nombre == Nombre("Laboratorio A-101", sufijo));
        result.Value.Should().NotContain(x => x.Nombre == Nombre("Auditorio Central", sufijo));
    }

    [Fact]
    public async Task TC_ESP_003_Filtro_de_estado_para_rol_de_gestion()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Coordinador));
        var filtro = new FiltroCatalogoBuilder().ConEstado(EstadoSpaceFiltro.Inactivo).Build();

        var result = await sut.HandleAsync(filtro);

        result.Value.Should().ContainSingle(x =>
            x.Nombre == Nombre("Auditorio Central", sufijo) && !x.Activo);
    }

    [Fact]
    public async Task TC_ESP_003_Administrador_tambien_puede_filtrar_inactivos()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Administrador));
        var filtro = new FiltroCatalogoBuilder().ConEstado(EstadoSpaceFiltro.Todos).Build();

        var result = await sut.HandleAsync(filtro);

        result.Value.Should().Contain(x => x.Nombre == Nombre("Auditorio Central", sufijo));
        result.Value.Should().Contain(x => x.Nombre == Nombre("Laboratorio A-101", sufijo));
    }

    [Fact]
    public async Task TC_ESP_003_Filtro_de_estado_denegado_a_solicitante()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Solicitante));
        var filtro = new FiltroCatalogoBuilder().ConEstado(EstadoSpaceFiltro.Inactivo).Build();

        var result = await sut.HandleAsync(filtro);

        result.Value.Should().NotContain(x => x.Nombre == Nombre("Auditorio Central", sufijo));
        result.Value.Should().OnlyContain(x => x.Activo);
    }

    [Fact]
    public async Task TC_ESP_003_Solicitante_no_puede_escapar_con_estado_todos()
    {
        await using var context = fixture.CreateContext();
        var sufijo = await SembrarAsync(context);
        var sut = new ConsultarCatalogo(new SpaceRepository(context), Usuario(Rol.Solicitante));
        var filtro = new FiltroCatalogoBuilder().ConEstado(EstadoSpaceFiltro.Todos).Build();

        var result = await sut.HandleAsync(filtro);

        result.Value.Should().OnlyContain(x => x.Activo);
        result.Value.Should().NotContain(x => x.Nombre == Nombre("Auditorio Central", sufijo));
    }

    private static IUsuarioActual Usuario(Rol rol) => new UsuarioActualStub(rol);

    private static string Nombre(string baseNombre, string sufijo) => $"{baseNombre}-{sufijo}";

    private static async Task<string> SembrarAsync(ReservaEspaciosDbContext context)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..6];
        context.Spaces.AddRange(
            Crear(Nombre("Laboratorio A-101", sufijo), EspacioTipo.Laboratorio, 30, activo: true),
            Crear(Nombre("Auditorio Central", sufijo), EspacioTipo.Auditorio, 200, activo: false),
            Crear(Nombre("Sala de Cómputo 2", sufijo), EspacioTipo.SalaComputo, 30, activo: true),
            Crear(Nombre("Sala pequeña", sufijo), EspacioTipo.SalaComputo, 8, activo: true));
        await context.SaveChangesAsync();
        return sufijo;
    }

    private static Space Crear(string nombre, EspacioTipo tipo, int capacidad, bool activo) => new(
        Guid.NewGuid(),
        nombre,
        tipo,
        capacidad,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)),
        activo);

    private sealed class UsuarioActualStub(Rol rol) : IUsuarioActual
    {
        public Rol Rol { get; } = rol;
    }
}
