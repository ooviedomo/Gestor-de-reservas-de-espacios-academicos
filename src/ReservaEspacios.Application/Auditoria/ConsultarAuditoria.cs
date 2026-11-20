using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Auditoria;

/// <summary>AUD-002: la misma Strategy de permisos que protege el resto de operaciones.</summary>
public sealed class ConsultarAuditoria(
    IAuditoriaRepository repository,
    GuardiaOperacionesProtegidas guardia)
{
    public async Task<Result<IReadOnlyList<AuditoriaDto>>> HandleAsync(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken = default)
    {
        var verificacion = guardia.VerificarAuditoria();
        return verificacion.IsSuccess
            ? Result<IReadOnlyList<AuditoriaDto>>.Success(
                await repository.ConsultarAsync(filtro, cancellationToken))
            : Result<IReadOnlyList<AuditoriaDto>>.Failure(verificacion.ErrorCode!);
    }
}
