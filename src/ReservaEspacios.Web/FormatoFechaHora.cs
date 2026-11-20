using System.Globalization;

namespace ReservaEspacios.Web;

/// <summary>
/// UI-005: convierte los instantes del dominio a la zona de la aplicación antes de
/// presentarlos. Es presentación pura: no modifica los <see cref="DateTimeOffset"/>
/// originales ni participa en la semántica del intervalo semiabierto.
/// </summary>
public static class FormatoFechaHora
{
    private static readonly TimeZoneInfo ZonaBogota =
        TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    private const string PatronHora = "HH:mm";
    private const string PatronFecha = "dd/MM/yyyy";
    private const string PatronFechaHora = "dd/MM/yyyy HH:mm";
    private const string SinDato = "—";

    public static string Hora(DateTimeOffset valor) =>
        EnBogota(valor).ToString(PatronHora, CultureInfo.InvariantCulture);

    public static string Fecha(DateTimeOffset valor) =>
        EnBogota(valor).ToString(PatronFecha, CultureInfo.InvariantCulture);

    public static string FechaHora(DateTimeOffset valor) =>
        EnBogota(valor).ToString(PatronFechaHora, CultureInfo.InvariantCulture);

    public static string Hora(DateTimeOffset? valor) => valor is { } instante ? Hora(instante) : SinDato;

    public static string Fecha(DateTimeOffset? valor) => valor is { } instante ? Fecha(instante) : SinDato;

    public static string FechaHora(DateTimeOffset? valor) => valor is { } instante ? FechaHora(instante) : SinDato;

    private static DateTimeOffset EnBogota(DateTimeOffset valor) =>
        TimeZoneInfo.ConvertTime(valor, ZonaBogota);
}
