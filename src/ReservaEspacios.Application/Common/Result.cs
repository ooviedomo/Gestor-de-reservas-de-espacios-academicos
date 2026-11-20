namespace ReservaEspacios.Application.Common;

/// <summary>Resultado de un caso de uso sin valor de retorno.</summary>
public sealed record Result
{
    public bool IsSuccess { get; private init; }
    public string? ErrorCode { get; private init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string errorCode) => new() { ErrorCode = errorCode };
}

/// <summary>Resultado de un caso de uso con valor de retorno.</summary>
public sealed record Result<T>
{
    public bool IsSuccess { get; private init; }
    public string? ErrorCode { get; private init; }
    public T? Value { get; private init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string errorCode) => new() { ErrorCode = errorCode };
}
