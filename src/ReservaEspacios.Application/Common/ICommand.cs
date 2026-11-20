namespace ReservaEspacios.Application.Common;

/// <summary>
/// Patrón Command (ADR-002): cada caso de uso del change es un comando con un único
/// manejador, lo que permite registrarlos en DI y probarlos de forma aislada.
/// </summary>
public interface ICommand;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
