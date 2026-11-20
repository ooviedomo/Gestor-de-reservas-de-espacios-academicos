namespace ReservaEspacios.Domain.Reservas;

/// <summary>Acciones que pueden solicitarse sobre una reserva.</summary>
internal enum TipoTransicion
{
    Aprobar,
    Rechazar,
    Cancelar,
    Completar
}
