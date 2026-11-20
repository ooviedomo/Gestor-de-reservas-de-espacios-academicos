using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Espacios;

public sealed record EditarSpaceCommand(
    Guid Id,
    string Nombre,
    EspacioTipo Tipo,
    int Capacidad,
    string Ubicacion,
    TimeOnly Apertura,
    TimeOnly Cierre);

/// <summary>ESP-002: actualiza los atributos de un espacio sin tocar sus reservas.</summary>
public sealed class EditarSpace(ISpaceRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(
        EditarSpaceCommand command,
        CancellationToken cancellationToken = default)
    {
        var space = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (space is null)
        {
            return Result.Failure(DomainErrors.EspacioNoEncontrado);
        }

        var existente = await repository.GetByNombreAsync(command.Nombre, cancellationToken);
        if (existente is not null && existente.Id != space.Id)
        {
            return Result.Failure(DomainErrors.NombreDuplicado);
        }

        try
        {
            var horario = new HorarioAtencion(command.Apertura, command.Cierre);
            space.Editar(command.Nombre, command.Tipo, command.Capacidad, command.Ubicacion, horario);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Result.Failure(exception.Code);
        }
    }
}
