namespace SharedKernel;

public enum ErrorType
{
    None = 0,
    Failure = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4,
    Validation = 5
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, string? error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, ErrorType.None);
    public static Result Failure(string error) => new(false, error, ErrorType.Failure);
    public static Result NotFound(string error = "Not found.") => new(false, error, ErrorType.NotFound);
    public static Result Forbidden(string error = "Forbidden.") => new(false, error, ErrorType.Forbidden);
    public static Result Conflict(string error = "Conflict.") => new(false, error, ErrorType.Conflict);
    public static Result ValidationFailure(string error) => new(false, error, ErrorType.Validation);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, string? error, ErrorType errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null, ErrorType.None);
    public static new Result<T> Failure(string error) => new(default, false, error, ErrorType.Failure);
    public static new Result<T> NotFound(string error = "Not found.") => new(default, false, error, ErrorType.NotFound);
    public static new Result<T> Forbidden(string error = "Forbidden.") => new(default, false, error, ErrorType.Forbidden);
    public static new Result<T> Conflict(string error = "Conflict.") => new(default, false, error, ErrorType.Conflict);
    public static new Result<T> ValidationFailure(string error) => new(default, false, error, ErrorType.Validation);
}
