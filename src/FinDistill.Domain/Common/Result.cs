namespace FinDistill.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error associated with a failed result. <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result with no value.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result with the given error.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>Creates a failed result of type <typeparamref name="T"/> with the given error.</summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the operation result value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");
}
