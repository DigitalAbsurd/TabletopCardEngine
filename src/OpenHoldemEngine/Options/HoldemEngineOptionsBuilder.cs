namespace OpenHoldemEngine.Options;
public class HoldemEngineOptionsBuilder
{
    public HoldemEngineOptions Options { get; set; } = new();

    public HoldemEngineOptionsBuilder WithSettings(Action<HoldemEngineSettings> configureSettings)
    {
        configureSettings.Invoke(Options.Settings);
        return this;
    }
}
