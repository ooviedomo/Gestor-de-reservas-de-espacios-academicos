using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;

namespace ReservaEspacios.Application.Autorizacion;

/// <summary>
/// Guard central de las operaciones protegidas que aún no tienen caso de uso propio
/// en este change (AUTH-001/002/003). Los handlers futuros de CrearReserva,
/// AprobarReserva, auditoría y reportes lo invocarán al inicio; así la matriz se
/// evalúa siempre en el mismo lugar y nunca se duplica en la capa HTTP.
/// </summary>
public sealed class GuardiaOperacionesProtegidas(
    IEvaluadorPermisos permisos,
    IUsuarioActual usuarioActual)
{
    public Result VerificarCrearReserva() => Verificar(permisos.PuedeCrearReserva(usuarioActual.Rol));

    public Result VerificarAprobar() => Verificar(permisos.PuedeAprobar(usuarioActual.Rol));

    public Result VerificarCompletar() => Verificar(permisos.PuedeCompletar(usuarioActual.Rol));

    public Result VerificarHistorial() =>
        usuarioActual.EstaAutenticado ? Result.Success() : Result.Failure(DomainErrors.NoAutenticado);

    public Result VerificarAdministrarUsuarios() =>
        Verificar(permisos.PuedeAdministrarUsuarios(usuarioActual.Rol));

    public Result VerificarAuditoria() => Verificar(permisos.PuedeConsultarAuditoria(usuarioActual.Rol));

    public Result VerificarReportes() => Verificar(permisos.PuedeConsultarReportes(usuarioActual.Rol));

    private Result Verificar(bool permitido) =>
        !usuarioActual.EstaAutenticado
            ? Result.Failure(DomainErrors.NoAutenticado)
            : permitido
                ? Result.Success()
                : Result.Failure(DomainErrors.NoAutorizado);
}
