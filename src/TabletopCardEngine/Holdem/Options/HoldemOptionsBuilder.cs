namespace TabletopCardEngine.Holdem.Options;

/// <summary>
/// Configures the engine from <c>HoldemBuilderExtensions.AddHoldem</c>.
/// <para>
/// Each builder writes to the engine options of the one container it was created for, so
/// configuring one engine leaves every other engine in the process untouched.
/// </para>
/// </summary>
public sealed class HoldemOptionsBuilder
{
    private readonly HoldemOptions _options;

    internal HoldemOptionsBuilder(HoldemOptions options)
    {
        _options = options;
    }

    /// <summary>Adjusts the validation limits applied to every game.</summary>
    /// <param name="configureSettings">Receives the settings to mutate.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The settings end up with a minimum below 2, a maximum above 10 (the largest table format, which a
    /// setting can only narrow), or a minimum above the maximum. That is a configuration error, not a game result.
    /// </exception>
    public HoldemOptionsBuilder WithSettings(Action<HoldemSettings> configureSettings)
    {
        configureSettings.Invoke(_options.Settings);

        var settings = _options.Settings;
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.GamePlayersMinCount, 2, nameof(configureSettings));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.GamePlayersMaxCount, 10, nameof(configureSettings));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.GamePlayersMinCount, settings.GamePlayersMaxCount, nameof(configureSettings));
        return this;
    }

    /// <summary>
    /// Replaces the random shuffle with pre-determined cards, which is what makes replaying a
    /// recorded hand history possible.
    /// </summary>
    /// <param name="cardShuffler">The source of pre-determined cards.</param>
    /// <returns>The same builder, for chaining.</returns>
    public HoldemOptionsBuilder WithCardShuffler(ICardShuffler cardShuffler)
    {
        _options.CardShuffler = cardShuffler;
        return this;
    }

    /// <summary>
    /// Replaces the clock tournaments read their blind levels from, which is <see cref="TimeProvider.System"/> by
    /// default. A test or a replay passes a clock it controls, so the levels change when it says.
    /// </summary>
    /// <param name="timeProvider">The clock to read.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public HoldemOptionsBuilder WithTimeProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _options.TimeProvider = timeProvider;
        return this;
    }
}
