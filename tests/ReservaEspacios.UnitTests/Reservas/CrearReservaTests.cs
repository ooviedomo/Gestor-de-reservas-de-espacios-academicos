using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Espacios;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Reservas.Validacion;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;
using ReservaEspacios.Domain.Reservas;
using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// RES-001/002 (tarea 3.2): el comando valida antes de abrir la transacción y
/// revalida la disponibilidad dentro de ella, antes de persistir.
/// </summary>
public sealed class CrearReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private readonly Guid _spaceId = Guid.NewGuid();
    private readonly Mock<ISolapeReservas> _solape = new();
    private readonly Mock<IReservaRepository> _repository = new();
    private readonly Mock<ITransaccionReserva> _transaccion = new();
    private readonly Mock<ISpaceRepository> _spaces = new();
    private readonly CrearReserva _sut;

    public CrearReservaTests()
    {
        _solape.Setup(x => x.HaySolapeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _spaces.Setup(x => x.GetByIdAsync(_spaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Espacio(_spaceId));
        _transaccion.Setup(x => x.EjecutarAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operacion, CancellationToken token) =>
                operacion(token));

        var validadorSolape = new ValidadorSolape(_solape.Object);
        var cadena = new CadenaValidacionCrearReserva(
            new ValidadorRango(),
            new ValidadorEspacioActivo(),
            new ValidadorHorario(new HorarioEspacio()),
            new ValidadorCapacidad(new ValidacionPorTipoEspacio()),
            validadorSolape);
        IUsuarioActual usuario = new UsuarioSolicitante(UsuarioId);
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), usuario);

        _sut = new CrearReserva(
            _repository.Object, _spaces.Object, _transaccion.Object,
            cadena, validadorSolape, guardia, usuario);
    }

    [Fact]
    public async Task TC_RES_001_Solicitud_valida_crea_reserva_pendiente_en_transaccion()
    {
        var resultado = await _sut.HandleAsync(Comando());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeEmpty();
        _transaccion.Verify(x => x.EjecutarAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.AddAsync(
            It.Is<Reservation>(r => r.Estado == EstadoReserva.Pendiente &&
                                    r.SpaceId == _spaceId && r.UsuarioId == UsuarioId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TC_RES_001_Rango_invalido_no_abre_transaccion()
    {
        var resultado = await _sut.HandleAsync(Comando() with { Fin = Inicio });

        resultado.ErrorCode.Should().Be(DomainErrors.RangoInvalido);
        _transaccion.Verify(x => x.EjecutarAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_RES_005_Mas_de_cuatro_horas_devuelve_DURACION_MAXIMA_EXCEDIDA()
    {
        var resultado = await _sut.HandleAsync(
            Comando() with { Fin = Inicio.AddHours(4).AddMinutes(1) });

        resultado.ErrorCode.Should().Be(DomainErrors.DuracionMaximaExcedida);
        _repository.Verify(x => x.AddAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_RES_001_Capacidad_excedida_no_abre_transaccion()
    {
        var resultado = await _sut.HandleAsync(Comando() with { Asistentes = 35 });

        resultado.ErrorCode.Should().Be(DomainErrors.CapacidadExcedida);
        _transaccion.Verify(x => x.EjecutarAsync(
            It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_RES_001_Sin_autenticacion_devuelve_NO_AUTENTICADO()
    {
        var cadena = new CadenaValidacionCrearReserva(
            new ValidadorRango(), new ValidadorEspacioActivo(),
            new ValidadorHorario(new HorarioEspacio()),
            new ValidadorCapacidad(new ValidacionPorTipoEspacio()),
            new ValidadorSolape(_solape.Object));
        IUsuarioActual anonimo = new UsuarioSolicitante(UsuarioId, autenticado: false);
        var guardia = new GuardiaOperacionesProtegidas(new EvaluadorPermisos(), anonimo);
        var sut = new CrearReserva(
            _repository.Object, _spaces.Object, _transaccion.Object,
            cadena, new ValidadorSolape(_solape.Object), guardia, anonimo);

        var resultado = await sut.HandleAsync(Comando());

        resultado.ErrorCode.Should().Be(DomainErrors.NoAutenticado);
    }

    [Fact]
    public async Task TC_RES_001_Disponibilidad_cambiada_antes_de_confirmar_se_rechaza()
    {
        // La consulta informativa vio la franja libre; la revalidación dentro de la
        // transacción ve que otra reserva la ocupó.
        _solape.SetupSequence(x => x.HaySolapeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var resultado = await _sut.HandleAsync(Comando());

        resultado.ErrorCode.Should().Be(DomainErrors.SolapeDetectado);
        _repository.Verify(x => x.AddAsync(
            It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private CrearReservaCommand Comando() => new(
        _spaceId, Inicio, Inicio.AddHours(1), "Clase de laboratorio", 20);

    private static Space Espacio(Guid id) => new(
        id,
        "Laboratorio A-101",
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)));

    private sealed class UsuarioSolicitante(Guid id, bool autenticado = true) : IUsuarioActual
    {
        public Rol Rol => Rol.Solicitante;
        public Guid? UsuarioId => autenticado ? id : null;
    }
}
