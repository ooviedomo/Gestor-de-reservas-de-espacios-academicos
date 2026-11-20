using ReservaEspacios.Application.Common;

namespace ReservaEspacios.Application.Auditoria;

/// <summary>
/// Puerto append-only: no expone actualización ni borrado (AUD-001). El único método de
/// modificación existe para demostrar que la base de datos rechaza el intento.
/// </summary>
public interface IAuditoriaRepository
{
    Task AppendAsync(AuditoriaRegistro registro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditoriaDto>> ConsultarAsync(
        FiltroAuditoria filtro,
        CancellationToken cancellationToken = default);

    Task<Result> IntentarEliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
