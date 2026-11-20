using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Common;

/// <summary>
/// Puerto de seguridad para conocer el rol de quien ejecuta un caso de uso.
/// Mañana lo implementará el Adapter de Identity sin tocar Application (ADR-002).
/// </summary>
public interface IUsuarioActual
{
    Rol Rol { get; }
}
