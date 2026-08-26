namespace OpenHoldemEngine.Options;

/// <summary>
/// Configures the engine from <c>ServiceCollectionExtensions.AddHoldemEngine</c>.
/// <para>
/// Every builder writes to the same process-wide <see cref="HoldemEngineOptions"/> instance, so
/// configuring one engine mutates the options of every engine in the process.
/// </para>
/// </summary>
public sealed class HoldemEngineOptionsBuilder
{
    /// <summary>The process-wide options instance this builder writes to.</summary>
    public static HoldemEngineOptions Options => HoldemEngineOptions.Instance;

    /// <summary>Adjusts the validation limits applied to every game.</summary>
    /// <param name="configureSettings">Receives the settings to mutate.</param>
    /// <returns>The same builder, for chaining.</returns>
    public HoldemEngineOptionsBuilder WithSettings(Action<HoldemEngineSettings> configureSettings)
    {
        configureSettings.Invoke(Options.Settings);
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
        Options.CardShuffler = cardShuffler;
        return this;
    }
}
