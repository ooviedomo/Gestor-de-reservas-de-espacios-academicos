using FluentAssertions;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;
using Xunit;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>
/// RES-005: el límite de cuatro horas es un invariante de `Reservation`, no un eslabón
/// de la cadena de creación, para que ninguna vía de alta pueda eludirlo.
/// </summary>
public sealed class ReservaDuracionTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TC_RES_005_Cuatro_horas_exactas_crea_la_reserva()
    {
        var reserva = Crear(Inicio.AddHours(4));

        reserva.Inicio.Should().Be(Inicio);
        reserva.Fin.Should().Be(Inicio.AddHours(4));
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public void TC_RES_005_Mas_de_cuatro_horas_devuelve_DURACION_MAXIMA_EXCEDIDA()
    {
        var accion = () => Crear(Inicio.AddHours(4).AddMinutes(1));

        accion.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.DuracionMaximaExcedida);
    }

    [Fact]
    public void TC_RES_005_Rango_inverso_de_mas_de_cuatro_horas_mantiene_RANGO_INVALIDO()
    {
        var accion = () => Crear(Inicio.AddHours(-5));

        accion.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.RangoInvalido);
    }

    private static Reservation Crear(DateTimeOffset fin) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Inicio, fin, "Clase de laboratorio", 20);
}
