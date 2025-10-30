namespace ServiceWatcher.Utils;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value returned on success. Throws if operation failed.
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException($"Cannot access Value when operation failed: {Error}");
            return _value!;
        }
    }

    /// <summary>
    /// Gets the error message if operation failed. Null if succeeded.
    /// </summary>
    public string? Error { get; }

    private readonly T? _value;

    private Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && value == null)
            throw new ArgumentException("Value cannot be null for successful result");
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty for failed result");

        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value) => new Result<T>(true, value, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result<T> Failure(Exception exception) => new Result<T>(false, default, exception.Message);
}

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if operation failed. Null if succeeded.
    /// </summary>
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        if (!isSuccess && string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty for failed result");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new Result(true, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static Result Failure(string error) => new Result(false, error);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result Failure(Exception exception) => new Result(false, exception.Message);
}
