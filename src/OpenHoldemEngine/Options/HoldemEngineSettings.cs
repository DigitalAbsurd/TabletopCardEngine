namespace OpenHoldemEngine.Options;

/// <summary>Validation limits applied to every game the engine runs.</summary>
public sealed class HoldemEngineSettings
{
    /// <summary>Fewest seats a hand may be dealt to.</summary>
    public int GamePlayersMinCount { get; set; } = 2;

    /// <summary>Most seats a hand may be dealt to.</summary>
    public int GamePlayersMaxCount { get; set; } = 6;
}
