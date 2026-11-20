using ReservaEspacios.Domain.Usuarios;

namespace ReservaEspacios.Application.Autorizacion;

/// <summary>Implementación de la Strategy que delega en la matriz canónica de Dominio.</summary>
public sealed class EvaluadorPermisos : IEvaluadorPermisos
{
    public bool PuedeCrearReserva(Rol rol) => rol.PuedeCrearReserva();

    public bool PuedeCancelarReserva(Rol rol, Guid solicitanteId, Guid usuarioActualId) =>
        rol.PuedeCancelarReservaAjena() ||
        (rol.PuedeCancelarReservaPropia() && solicitanteId == usuarioActualId);

    public bool PuedeVerHistorial(Rol rol) => true;

    public bool PuedeVerHistorialCompleto(Rol rol) => rol.PuedeVerHistorialCompleto();

    public bool PuedeAprobar(Rol rol) => rol.PuedeAprobarReserva();

    public bool PuedeCompletar(Rol rol) => rol.PuedeCompletarReserva();

    public bool PuedeAdministrarEspacios(Rol rol) => rol.PuedeAdministrarEspacios();

    public bool PuedeAdministrarUsuarios(Rol rol) => rol.PuedeAdministrarUsuarios();

    public bool PuedeConsultarAuditoria(Rol rol) => rol.PuedeConsultarAuditoria();

    public bool PuedeConsultarReportes(Rol rol) => rol.PuedeConsultarReportes();
}
