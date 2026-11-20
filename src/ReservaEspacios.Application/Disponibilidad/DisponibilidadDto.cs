namespace ReservaEspacios.Application.Disponibilidad;

public static class EstadosDisponibilidad
{
    public const string Disponible = "DISPONIBLE";
    public const string NoDisponible = "NO_DISPONIBLE";
}

public static class MotivosDisponibilidad
{
    public const string Ninguno = "NINGUNO";
    public const string EspacioInactivo = "ESPACIO_INACTIVO";
    public const string FueraDeHorario = "FUERA_DE_HORARIO";
    public const string SolapeConReserva = "SOLAPE_CON_RESERVA";
}

/// <summary>DISP-001: estado de la franja y motivo legible por la UI.</summary>
public sealed record DisponibilidadDto(string Estado, string Motivo);
