using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Domain.Espacios;

/// <summary>
/// Horario de atención de un espacio. ESP-001 exige que la apertura sea anterior al cierre.
/// DISP-003 interpreta estos valores en America/Bogota antes de compararlos en UTC.
/// </summary>
public sealed record HorarioAtencion
{
    public TimeOnly Apertura { get; }
    public TimeOnly Cierre { get; }

    public HorarioAtencion(TimeOnly apertura, TimeOnly cierre)
    {
        if (apertura >= cierre)
        {
            throw new DomainException(
                DomainErrors.HorarioInvalido,
                "La hora de apertura debe ser anterior a la hora de cierre.");
        }

        Apertura = apertura;
        Cierre = cierre;
    }

    /// <summary>EF Core necesita un constructor sin parámetros para el owned type.</summary>
    private HorarioAtencion()
    {
    }
}
