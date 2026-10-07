namespace TabletopCardEngine;

/// <summary>
/// The database cannot be used as the engine is configured: a deployment fault, not a game result,
/// which is why it is an exception rather than a <see cref="HoldemErrorCode"/>.
/// </summary>
public sealed class HoldemSchemaException : InvalidOperationException
{
    /// <summary>Creates an exception with no message and no status.</summary>
    public HoldemSchemaException()
    {
    }

    /// <summary>Creates an exception with a message and no status.</summary>
    /// <param name="message">What went wrong.</param>
    public HoldemSchemaException(string? message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message, the exception that caused it, and no status.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public HoldemSchemaException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    internal HoldemSchemaException(HoldemSchemaFailure failure, string message, HoldemSchemaStatus? status)
        : base(message)
    {
        Failure = failure;
        Status = status;
    }

    /// <summary>
    /// Why the database cannot be used. An exception built with one of the public constructors
    /// carries <c>default</c>, which is not a defined member.
    /// </summary>
    public HoldemSchemaFailure Failure { get; }

    /// <summary>The schema status that led to the failure, or <c>null</c> when it was not read.</summary>
    public HoldemSchemaStatus? Status { get; }
}
