namespace FluxoCaixaDiario.SharedKernel.Result;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Result de sucesso não pode ter erro.");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Result de falha deve ter mensagem de erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);
}

public class Result<T> : Result
{
    private readonly T _value;

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Result de falha não possui valor.");

    internal Result(T value, bool isSuccess, string error) : base(isSuccess, error)
        => _value = value;
}