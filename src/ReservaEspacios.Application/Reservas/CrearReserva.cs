using ReservaEspacios.Application.Autorizacion;
using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Reservas;

namespace ReservaEspacios.Application.Reservas;

public sealed record CrearReservaCommand(
    Guid SpaceId,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    string Proposito,
    int Asistentes);

/// <summary>
/// RES-001/002: primera versión del caso de uso. La cadena de validadores y la
/// revalidación `SERIALIZABLE` se incorporan en la tarea 3.2; aquí se garantiza
/// que el error de la restricción llegue traducido a `SOLAPE_DETECTADO`.
/// </summary>
public sealed class CrearReserva(
    IReservaRepository repository,
    GuardiaOperacionesProtegidas guardia,
    IUsuarioActual usuarioActual)
{
    public async Task<Result<Guid>> HandleAsync(
        CrearReservaCommand command,
        CancellationToken cancellationToken = default)
    {
        var verificacion = guardia.VerificarCrearReserva();
        if (!verificacion.IsSuccess)
        {
            return Result<Guid>.Failure(verificacion.ErrorCode!);
        }

        if (usuarioActual.UsuarioId is not { } usuarioId)
        {
            return Result<Guid>.Failure(DomainErrors.NoAutenticado);
        }

        try
        {
            var reserva = new Reservation(
                Guid.NewGuid(),
                command.SpaceId,
                usuarioId,
                command.Inicio,
                command.Fin,
                command.Proposito,
                command.Asistentes);

            await repository.AddAsync(reserva, cancellationToken);
            return Result<Guid>.Success(reserva.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Code);
        }
    }
}
