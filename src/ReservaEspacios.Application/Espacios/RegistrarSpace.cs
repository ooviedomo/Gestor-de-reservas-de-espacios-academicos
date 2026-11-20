using ReservaEspacios.Application.Common;
using ReservaEspacios.Domain.Common;
using ReservaEspacios.Domain.Espacios;

namespace ReservaEspacios.Application.Espacios;

public sealed record RegistrarSpaceCommand(
    string Nombre,
    EspacioTipo Tipo,
    int Capacidad,
    string Ubicacion,
    TimeOnly Apertura,
    TimeOnly Cierre);

/// <summary>ESP-001: registra un espacio y devuelve códigos de error estables.</summary>
public sealed class RegistrarSpace(ISpaceRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> HandleAsync(
        RegistrarSpaceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetByNombreAsync(command.Nombre, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(DomainErrors.NombreDuplicado);
        }

        try
        {
            var horario = new HorarioAtencion(command.Apertura, command.Cierre);
            var space = new Space(
                Guid.NewGuid(),
                command.Nombre,
                command.Tipo,
                command.Capacidad,
                command.Ubicacion,
                horario);

            await repository.AddAsync(space, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(space.Id);
        }
        catch (DomainException exception)
        {
            return Result<Guid>.Failure(exception.Code);
        }
    }
}
