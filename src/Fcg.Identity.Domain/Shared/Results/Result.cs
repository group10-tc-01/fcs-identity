namespace Fcg.Identity.Domain.Shared.Results;

public readonly struct Result<TValue>
{
    private readonly TValue? _value;
    private readonly Error? _error;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);

    public TOutput Match<TOutput>(Func<TValue, TOutput> onSuccess, Func<Error, TOutput> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
