using FluentAssertions;
using ReservaEspacios.Application.Reservas;
using ReservaEspacios.Domain.Common;
using Xunit;

namespace ReservaEspacios.UnitTests.Reservas;

/// <summary>Tarea 2.3: el error de la restricción se convierte en un código estable de dominio.</summary>
public sealed class TraductorErroresReservaTests
{
    [Fact]
    public void TC_RES_002_Violacion_de_no_overlap_se_traduce_a_SOLAPE_DETECTADO()
    {
        var excepcion = TraductorErroresReserva.Traducir(
            TraductorErroresReserva.SqlStateExclusionViolation,
            TraductorErroresReserva.RestriccionSolape);

        excepcion.Should().NotBeNull();
        excepcion!.Code.Should().Be(DomainErrors.SolapeDetectado);
        excepcion.Message.Should().NotContain(TraductorErroresReserva.SqlStateExclusionViolation);
        excepcion.Message.Should().NotContain(TraductorErroresReserva.RestriccionSolape);
    }

    [Fact]
    public void TC_RES_002_Otra_restriccion_no_se_traduce()
    {
        TraductorErroresReserva.Traducir("23P01", "otra_restriccion").Should().BeNull();
    }

    [Fact]
    public void TC_RES_002_Otro_sqlstate_no_se_traduce()
    {
        TraductorErroresReserva.Traducir("23514", TraductorErroresReserva.RestriccionSolape)
            .Should().BeNull();
    }

    [Fact]
    public void TC_RES_002_Ausencia_de_datos_no_se_traduce()
    {
        TraductorErroresReserva.Traducir(null, null).Should().BeNull();
    }
}
