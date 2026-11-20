using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reportes;

/// <summary>
/// REP-001/002/003 y patrones de ADR-002: Builder y Strategy de cálculo de ocupación.
/// </summary>
public sealed class ReporteTests
{
    private static readonly DateTimeOffset Desde =
        new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Hasta =
        new(2026, 11, 21, 0, 0, 0, TimeSpan.Zero);

    private readonly Guid _espacioId = Guid.NewGuid();
    private readonly Mock<IRepositorioOcupacion> _repositorio = new();

    [Fact]
    public void TC_REP_003_Denominador_usa_dos_dias_naturales_de_Bogota_y_da_20_horas()
    {
        var datos = Datos(
            new IntervaloReserva(
                new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero)));

        var resultado = new CalculoOcupacion().Calcular(datos, Filtro());

        resultado.HorasDisponibles.Should().Be(20);
        resultado.HorasReservadas.Should().Be(1);
        resultado.PorcentajeOcupacion.Should().Be(5);
    }

    [Fact]
    public void TC_REP_003_Intervalos_solapados_no_se_duplican()
    {
        var datos = Datos(
            new IntervaloReserva(
                new DateTimeOffset(2026, 11, 20, 15, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero)),
            new IntervaloReserva(
                new DateTimeOffset(2026, 11, 20, 15, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 20, 16, 30, 0, TimeSpan.Zero)));

        var resultado = new CalculoOcupacion().Calcular(datos, Filtro());

        resultado.HorasReservadas.Should().Be(1.5);
        resultado.PorcentajeOcupacion.Should().Be(7.5);
    }

    [Fact]
    public void TC_REP_003_Sin_reservas_el_porcentaje_es_cero_y_no_divide_por_cero()
    {
        var datos = Datos();

        var resultado = new CalculoOcupacion().Calcular(datos, Filtro());

        resultado.HorasReservadas.Should().Be(0);
        resultado.PorcentajeOcupacion.Should().Be(0);
    }

    [Fact]
    public async Task TC_REP_001_Reporte_devuelve_solo_el_espacio_solicitado()
    {
        var filtro = new ReporteBuilder().Desde(Desde).Hasta(Hasta).ParaEspacio(_espacioId).Build();
        _repositorio.Setup(x => x.ObtenerPorPeriodoAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Datos()]);
        var sut = CrearConsulta(Rol.Coordinador);

        var resultado = await sut.HandleAsync(filtro);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Espacios.Should().ContainSingle()
            .Which.EspacioId.Should().Be(_espacioId);
        _repositorio.Verify(x => x.ObtenerPorPeriodoAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_REP_001_Rango_invalido_devuelve_RANGO_INVALIDO()
    {
        var filtro = new ReporteBuilder().Desde(Hasta).Hasta(Desde).Build();

        var resultado = await CrearConsulta(Rol.Coordinador).HandleAsync(filtro);

        resultado.ErrorCode.Should().Be(DomainErrors.RangoInvalido);
        _repositorio.Verify(
            x => x.ObtenerPorPeriodoAsync(It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public async Task TC_REP_002_Rol_con_permiso_consulta_el_reporte(Rol rol)
    {
        _repositorio.Setup(x => x.ObtenerPorPeriodoAsync(
                It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await CrearConsulta(rol).HandleAsync(Filtro());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Espacios.Should().BeEmpty();
    }

    [Fact]
    public async Task TC_REP_002_Solicitante_no_puede_consultar_el_reporte()
    {
        var sut = CrearConsulta(Rol.Solicitante);

        var resultado = await sut.HandleAsync(Filtro());

        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _repositorio.Verify(
            x => x.ObtenerPorPeriodoAsync(It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void TC_REP_001_Builder_construye_el_filtro_con_valores_explicitos()
    {
        var filtro = new ReporteBuilder()
            .Desde(Desde)
            .Hasta(Hasta)
            .ParaEspacio(_espacioId)
            .Build();

        filtro.Desde.Should().Be(Desde);
        filtro.Hasta.Should().Be(Hasta);
        filtro.SpaceId.Should().Be(_espacioId);
        new ReporteBuilder().Build().SpaceId.Should().BeNull();
    }

    private ConsultarReporteOcupacion CrearConsulta(Rol rol) => new(
        _repositorio.Object,
        new CalculoOcupacion(),
        new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(rol)));

    private IUsuarioActual Usuario(Rol rol) => new UsuarioDePrueba(rol, Guid.NewGuid());

    private FiltroReporte Filtro() => new(_espacioId, Desde, Hasta);

    private DatosOcupacionEspacio Datos(params IntervaloReserva[] intervalos) => new(
        _espacioId, "Laboratorio A-101", new TimeOnly(8, 0), new TimeOnly(18, 0),
        intervalos.Length, intervalos);

    private sealed record UsuarioDePrueba(Rol Rol, Guid? UsuarioId) : IUsuarioActual;
}
