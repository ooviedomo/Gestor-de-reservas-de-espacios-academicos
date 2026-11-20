using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Autorizacion;

/// <summary>
/// Strategy (ADR-002): encapsula la matriz de permisos de AUTH-001/002/003 para que
/// los guards no conozcan las reglas y la UI pueda consultarlas sin duplicarlas.
/// </summary>
public interface IEvaluadorPermisos
{
    bool PuedeCrearReserva(Rol rol);
    bool PuedeCancelarReserva(Rol rol, Guid solicitanteId, Guid usuarioActualId);
    bool PuedeVerHistorial(Rol rol);
    bool PuedeVerHistorialCompleto(Rol rol);
    bool PuedeAprobar(Rol rol);
    bool PuedeCompletar(Rol rol);
    bool PuedeAdministrarEspacios(Rol rol);
    bool PuedeAdministrarUsuarios(Rol rol);
    bool PuedeConsultarAuditoria(Rol rol);
    bool PuedeConsultarReportes(Rol rol);
}
