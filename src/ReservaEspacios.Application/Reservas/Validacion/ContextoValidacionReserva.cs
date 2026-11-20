using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Reservas.Validacion;

/// <summary>Datos que comparten todos los eslabones de la cadena de RES-001.</summary>
public sealed record ContextoValidacionReserva(
    CrearReservaCommand Command,
    Guid UsuarioId,
    Space? Space);
