using FluentAssertions;
using ReservaEspacios.Web;
using Xunit;

namespace ReservaEspacios.E2ETests;

/// <summary>UI-005: el formateador es presentación pura y usa siempre America/Bogota.</summary>
public sealed class FormatoFechaHoraTests
{
    private static readonly DateTimeOffset Instante =
        new(2026, 11, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TC_UI_005_Formatea_hora_fecha_y_fecha_hora_en_zona_bogota()
    {
        FormatoFechaHora.Hora(Instante).Should().Be("10:00");
        FormatoFechaHora.Fecha(Instante).Should().Be("20/11/2026");
        FormatoFechaHora.FechaHora(Instante).Should().Be("20/11/2026 10:00");
    }

    [Fact]
    public void TC_UI_005_Valores_nulos_muestran_guion()
    {
        DateTimeOffset? vacio = null;

        FormatoFechaHora.Hora(vacio).Should().Be("—");
        FormatoFechaHora.Fecha(vacio).Should().Be("—");
        FormatoFechaHora.FechaHora(vacio).Should().Be("—");
    }
}
