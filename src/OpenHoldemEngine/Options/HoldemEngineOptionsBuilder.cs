namespace OpenHoldemEngine.Options;

/// <summary>
/// Configures the engine from <c>ServiceCollectionExtensions.AddHoldemEngine</c>.
/// <para>
/// Each builder writes to the <see cref="HoldemEngineOptions"/> instance of the one container it
/// was created for, so configuring one engine leaves every other engine in the process untouched.
/// </para>
/// </summary>
public sealed class HoldemEngineOptionsBuilder
{
    private readonly HoldemEngineOptions _options;

    internal HoldemEngineOptionsBuilder(HoldemEngineOptions options)
    {
        _options = options;
    }

    /// <summary>Adjusts the validation limits applied to every game.</summary>
    /// <param name="configureSettings">Receives the settings to mutate.</param>
    /// <returns>The same builder, for chaining.</returns>
    public HoldemEngineOptionsBuilder WithSettings(Action<HoldemEngineSettings> configureSettings)
    {
        configureSettings.Invoke(_options.Settings);
        return this;
    }

    /// <summary>
    /// Replaces the random shuffle with pre-determined cards, which is what makes replaying a
    /// recorded hand history possible.
    /// </summary>
    /// <param name="cardShuffler">The source of pre-determined cards.</param>
    /// <returns>The same builder, for chaining.</returns>
    public HoldemEngineOptionsBuilder WithCardShuffler(ICardShuffler cardShuffler)
    {
        _options.CardShuffler = cardShuffler;
        return this;
    }
}
