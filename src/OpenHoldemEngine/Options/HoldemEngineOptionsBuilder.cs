namespace OpenHoldemEngine.Options;
public class HoldemEngineOptionsBuilder
{
    public HoldemEngineOptions Options => HoldemEngineOptions.Instance;

    public HoldemEngineOptionsBuilder WithSettings(Action<HoldemEngineSettings> configureSettings)
    {
        configureSettings.Invoke(Options.Settings);
        return this;
    }

    public HoldemEngineOptionsBuilder WithCardShuffler(ICardShuffler cardShuffler)
    {
        Options.CardShuffler = cardShuffler;
        return this;
    }
}
