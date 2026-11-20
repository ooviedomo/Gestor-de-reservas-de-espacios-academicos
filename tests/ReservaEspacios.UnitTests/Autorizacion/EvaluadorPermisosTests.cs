using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Autorizacion;

/// <summary>AUTH-001/002/003: la matriz canónica se evalúa igual para todos los roles.</summary>
public sealed class EvaluadorPermisosTests
{
    private readonly IEvaluadorPermisos _permisos = new EvaluadorPermisos();

    [Theory]
    [InlineData(Rol.Solicitante, true)]
    [InlineData(Rol.Coordinador, false)]
    [InlineData(Rol.Administrador, false)]
    public void TC_AUTH_002_Solo_solicitante_crea_reservas(Rol rol, bool esperado) =>
        _permisos.PuedeCrearReserva(rol).Should().Be(esperado);

    [Fact]
    public void TC_AUTH_002_Solicitante_cancela_solo_las_propias()
    {
        var usuario = Guid.NewGuid();
        var ajena = Guid.NewGuid();

        _permisos.PuedeCancelarReserva(Rol.Solicitante, usuario, usuario).Should().BeTrue();
        _permisos.PuedeCancelarReserva(Rol.Solicitante, ajena, usuario).Should().BeFalse();
        _permisos.PuedeCancelarReserva(Rol.Coordinador, ajena, usuario).Should().BeTrue();
        _permisos.PuedeCancelarReserva(Rol.Administrador, ajena, usuario).Should().BeTrue();
    }

    [Theory]
    [InlineData(Rol.Solicitante, false)]
    [InlineData(Rol.Coordinador, true)]
    [InlineData(Rol.Administrador, true)]
    public void TC_AUTH_001_Aprobar_y_completar_solo_gestion(Rol rol, bool esperado)
    {
        _permisos.PuedeAprobar(rol).Should().Be(esperado);
        _permisos.PuedeCompletar(rol).Should().Be(esperado);
    }

    [Theory]
    [InlineData(Rol.Solicitante, false)]
    [InlineData(Rol.Coordinador, true)]
    [InlineData(Rol.Administrador, true)]
    public void TC_AUTH_003_Auditoria_y_reportes_solo_gestion(Rol rol, bool esperado)
    {
        _permisos.PuedeConsultarAuditoria(rol).Should().Be(esperado);
        _permisos.PuedeConsultarReportes(rol).Should().Be(esperado);
        _permisos.PuedeVerHistorialCompleto(rol).Should().Be(esperado);
    }

    [Theory]
    [InlineData(Rol.Solicitante, false)]
    [InlineData(Rol.Coordinador, false)]
    [InlineData(Rol.Administrador, true)]
    public void TC_AUTH_003_Solo_administrador_administra_espacios_y_usuarios(
        Rol rol, bool esperado)
    {
        _permisos.PuedeAdministrarEspacios(rol).Should().Be(esperado);
        _permisos.PuedeAdministrarUsuarios(rol).Should().Be(esperado);
    }

    [Theory]
    [InlineData(Rol.Solicitante)]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public void TC_AUTH_002_Todos_pueden_consultar_disponibilidad_y_su_historial(Rol rol)
    {
        _permisos.PuedeVerHistorial(rol).Should().BeTrue();
    }
}

public sealed class GuardiaOperacionesProtegidasTests
{
    [Theory]
    [InlineData(Rol.Solicitante, DomainErrors.NoAutorizado)]
    [InlineData(Rol.Coordinador, null)]
    [InlineData(Rol.Administrador, null)]
    public void TC_AUTH_001_Guard_de_aprobacion_devuelve_NO_AUTORIZADO(
        Rol rol, string? errorEsperado)
    {
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(rol));

        var resultado = guardia.VerificarAprobar();

        ResultEsperado(resultado, errorEsperado);
    }

    [Theory]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.Administrador)]
    public void TC_AUTH_001_Guard_de_creacion_de_reserva_deniega_a_gestion(Rol rol)
    {
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(rol));

        ResultEsperado(guardia.VerificarCrearReserva(), DomainErrors.NoAutorizado);
    }

    [Fact]
    public void TC_AUTH_001_Guard_deniega_acceso_anonimo_con_NO_AUTENTICADO()
    {
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(null));

        ResultEsperado(guardia.VerificarHistorial(), DomainErrors.NoAutenticado);
        ResultEsperado(guardia.VerificarCrearReserva(), DomainErrors.NoAutenticado);
        ResultEsperado(guardia.VerificarAprobar(), DomainErrors.NoAutenticado);
        ResultEsperado(guardia.VerificarAuditoria(), DomainErrors.NoAutenticado);
    }

    [Fact]
    public void TC_AUTH_001_Guard_de_historial_permite_cualquier_rol_autenticado()
    {
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), Usuario(Rol.Solicitante));

        guardia.VerificarHistorial().IsSuccess.Should().BeTrue();
    }

    private static void ResultEsperado(Result resultado, string? errorEsperado)
    {
        if (errorEsperado is null)
        {
            resultado.IsSuccess.Should().BeTrue();
        }
        else
        {
            resultado.IsSuccess.Should().BeFalse();
            resultado.ErrorCode.Should().Be(errorEsperado);
        }
    }

    private static IUsuarioActual Usuario(Rol? rol)
    {
        var mock = new Mock<IUsuarioActual>();
        mock.SetupGet(x => x.Rol).Returns(rol ?? Rol.Solicitante);
        mock.SetupGet(x => x.EstaAutenticado).Returns(rol is not null);
        mock.SetupGet(x => x.UsuarioId).Returns(rol is null ? null : Guid.NewGuid());
        return mock.Object;
    }
}
