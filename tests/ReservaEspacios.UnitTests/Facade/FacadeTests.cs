using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Auditoria;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Facade;
using ReservaEspacios.Application.Reportes;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Facade;

/// <summary>
/// Facade (ADR-002): los componentes Razor consultan una única puerta que expone
/// catálogo, disponibilidad, auditoría y reporte sin conocer los casos de uso internos.
/// </summary>
public sealed class FacadeTests
{
    private readonly Mock<IConsultarCatalogo> _catalogo = new();
    private readonly Mock<IConsultarDisponibilidad> _disponibilidad = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();
    private readonly Mock<IRepositorioOcupacion> _ocupacion = new();

    [Fact]
    public async Task TC_FACADE_ConsultarCatalogo_y_disponibilidad_delegan_en_los_casos_de_uso()
    {
        var filtro = new FiltroCatalogo(null, null, EstadoSpaceFiltro.Activo);
        var comando = new ConsultarDisponibilidadCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var catalogo = Result<IReadOnlyList<SpaceCatalogoDto>>.Success([]);
        var disponibilidad = Result<DisponibilidadDto>.Success(new DisponibilidadDto(
            EstadosDisponibilidad.Disponible, MotivosDisponibilidad.Ninguno));
        _catalogo.Setup(x => x.HandleAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogo);
        _disponibilidad.Setup(x => x.HandleAsync(comando, It.IsAny<CancellationToken>()))
            .ReturnsAsync(disponibilidad);
        var sut = CrearFacade(Rol.Solicitante);

        var resultadoCatalogo = await sut.ConsultarCatalogo(filtro);
        var resultadoDisponibilidad = await sut.ConsultarDisponibilidad(comando);

        resultadoCatalogo.Should().BeSameAs(catalogo);
        resultadoDisponibilidad.Should().BeSameAs(disponibilidad);
    }

    [Fact]
    public async Task TC_FACADE_ConsultarAuditoria_delega_en_el_caso_de_uso_protegido()
    {
        var filtro = new FiltroAuditoria("Reservation");
        var esperado = Result<IReadOnlyList<AuditoriaDto>>.Success([]);
        _auditoria.Setup(x => x.ConsultarAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado.Value!);
        var sut = CrearFacade(Rol.Coordinador);

        var resultado = await sut.ConsultarAuditoria(filtro);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task TC_FACADE_GenerarReporte_delega_en_el_caso_de_uso_protegido()
    {
        var filtro = new FiltroReporte(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        var sut = CrearFacade(Rol.Administrador);
        _ocupacion.Setup(x => x.ObtenerPorPeriodoAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await sut.GenerarReporte(filtro);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Espacios.Should().BeEmpty();
        _ocupacion.Verify(x => x.ObtenerPorPeriodoAsync(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_FACADE_El_solicitante_recibe_NO_AUTORIZADO_al_usar_la_facade()
    {
        var sut = CrearFacade(Rol.Solicitante);

        var auditoria = await sut.ConsultarAuditoria(new FiltroAuditoria());
        var reporte = await sut.GenerarReporte(new FiltroReporte(
            null, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));

        auditoria.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        reporte.ErrorCode.Should().Be(DomainErrors.NoAutorizado);
        _auditoria.Verify(
            x => x.ConsultarAsync(It.IsAny<FiltroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _ocupacion.Verify(
            x => x.ObtenerPorPeriodoAsync(It.IsAny<FiltroReporte>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private ReservaEspaciosFacade CrearFacade(Rol rol)
    {
        var usuario = new UsuarioDePrueba(rol, Guid.NewGuid());
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), usuario);
        return new ReservaEspaciosFacade(
            _catalogo.Object,
            _disponibilidad.Object,
            new ConsultarAuditoria(_auditoria.Object, guardia),
            new ConsultarReporteOcupacion(
                _ocupacion.Object, new CalculoOcupacion(), guardia));
    }

    private sealed record UsuarioDePrueba(Rol Rol, Guid? UsuarioId) : IUsuarioActual;
}
