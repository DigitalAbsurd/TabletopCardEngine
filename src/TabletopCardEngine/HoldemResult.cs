namespace TabletopCardEngine;

/// <summary>
/// The outcome of one engine call: a value on success, or the errors that rejected the action.
/// </summary>
/// <typeparam name="T">The type of the value carried on success.</typeparam>
public sealed class HoldemResult<T>
{
    private readonly T? _value;

    private HoldemResult(T? value, IReadOnlyList<HoldemError> errors)
    {
        _value = value;
        Errors = errors;
    }

    /// <summary>Whether the action was applied.</summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>Whether the action was rejected.</summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>The reasons the action was rejected; empty on success.</summary>
    public IReadOnlyList<HoldemError> Errors { get; }

    /// <summary>
    /// The value of a successful call.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result is failed, so there is no value.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The result is failed, so it has no value.");

    internal static HoldemResult<T> Ok(T value) => new(value, []);

    internal static HoldemResult<T> Fail(params HoldemError[] errors)
        => errors.Length == 0
            ? throw new ArgumentException("A failed result needs at least one error.", nameof(errors))
            : new(default, errors);
}
