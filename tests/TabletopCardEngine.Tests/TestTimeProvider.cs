namespace TabletopCardEngine.Tests;

/// <summary>A clock a test sets by hand, so the tournament's level clock can be read at any instant.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    /// <summary>The instant <see cref="GetUtcNow"/> returns; a test moves it forward with <see cref="Advance"/>.</summary>
    internal DateTimeOffset UtcNow { get; set; } = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;

    internal void Advance(TimeSpan delta) => UtcNow += delta;
}
