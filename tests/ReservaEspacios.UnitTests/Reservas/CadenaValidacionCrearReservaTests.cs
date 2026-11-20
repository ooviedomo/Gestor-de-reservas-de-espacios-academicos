using FluentAssertions;
using Moq;
using ReservaEspacios.Application.Disponibilidad;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Application.Reservas.Validacion;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// ADR-002 / tarea 3.2: la cadena de RES-001 se detiene en el primer error y aplica
/// los eslabones en el orden Rango → EspacioActivo → Horario → Capacidad → Solape.
/// </summary>
public sealed class CadenaValidacionCrearReservaTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);
    private readonly Mock<ISolapeReservas> _solape = new();
    private readonly CadenaValidacionCrearReserva _sut;

    public CadenaValidacionCrearReservaTests()
    {
        _solape.Setup(x => x.HaySolapeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _sut = new CadenaValidacionCrearReserva(
            new ValidadorRango(),
            new ValidadorEspacioActivo(),
            new ValidadorHorario(new HorarioEspacio()),
            new ValidadorCapacidad(new ValidacionPorTipoEspacio()),
            new ValidadorSolape(_solape.Object));
    }

    [Fact]
    public async Task TC_RES_001_Cadena_completa_sin_errores_devuelve_null()
    {
        var error = await _sut.ValidarAsync(Contexto());

        error.Should().BeNull();
    }

    [Fact]
    public async Task TC_RES_001_Rango_invalido_detiene_la_cadena_antes_de_consultar_solape()
    {
        var contexto = Contexto() with { Command = Comando() with { Fin = Inicio } };

        var error = await _sut.ValidarAsync(contexto);

        error.Should().Be(DomainErrors.RangoInvalido);
        _solape.Verify(x => x.HaySolapeAsync(
            It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TC_RES_001_Espacio_inexistente_devuelve_ESPACIO_NO_ENCONTRADO()
    {
        var error = await _sut.ValidarAsync(Contexto() with { Space = null });

        error.Should().Be(DomainErrors.EspacioNoEncontrado);
    }

    [Fact]
    public async Task TC_RES_001_Espacio_inactivo_devuelve_ESPACIO_INACTIVO()
    {
        var error = await _sut.ValidarAsync(Contexto(activo: false));

        error.Should().Be(DomainErrors.EspacioInactivo);
    }

    [Fact]
    public async Task TC_RES_001_Fuera_de_horario_devuelve_FUERA_DE_HORARIO()
    {
        var contexto = Contexto() with
        {
            Command = Comando() with
            {
                Inicio = new DateTimeOffset(2026, 11, 21, 1, 0, 0, TimeSpan.Zero),
                Fin = new DateTimeOffset(2026, 11, 21, 2, 0, 0, TimeSpan.Zero)
            }
        };

        var error = await _sut.ValidarAsync(contexto);

        error.Should().Be(DomainErrors.FueraDeHorario);
    }

    [Fact]
    public async Task TC_RES_001_Capacidad_excedida_devuelve_CAPACIDAD_EXCEDIDA()
    {
        var contexto = Contexto() with { Command = Comando() with { Asistentes = 35 } };

        var error = await _sut.ValidarAsync(contexto);

        error.Should().Be(DomainErrors.CapacidadExcedida);
    }

    [Fact]
    public async Task TC_RES_001_Solape_devuelve_SOLAPE_DETECTADO()
    {
        _solape.Setup(x => x.HaySolapeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var error = await _sut.ValidarAsync(Contexto());

        error.Should().Be(DomainErrors.SolapeDetectado);
    }

    private static ContextoValidacionReserva Contexto(bool activo = true) => new(
        Comando(), Guid.NewGuid(), Espacio(activo));

    private static CrearReservaCommand Comando() => new(
        Guid.NewGuid(), Inicio, Inicio.AddHours(1), "Clase de laboratorio", 20);

    private static Space Espacio(bool activo) => new(
        Guid.NewGuid(),
        "Laboratorio A-101",
        EspacioTipo.Laboratorio,
        30,
        "Bloque A",
        new HorarioAtencion(new TimeOnly(8, 0), new TimeOnly(18, 0)),
        activo);
}
