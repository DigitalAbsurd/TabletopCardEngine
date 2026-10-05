namespace OpenHoldemEngine.Options;

/// <summary>Validation limits applied to every game the engine runs.</summary>
public sealed class HoldemEngineSettings
{
    /// <summary>Fewest seats a hand may be dealt to. Within 2 to 6 in this release; <c>WithSettings</c> throws outside that range.</summary>
    public int GamePlayersMinCount { get; set; } = 2;

    /// <summary>Most seats a hand may be dealt to. Within 2 to 6 in this release; <c>WithSettings</c> throws outside that range.</summary>
    public int GamePlayersMaxCount { get; set; } = 6;

    /// <summary>
    /// When <c>true</c>, the engine logs every seat's hole cards and the board at Trace level.
    /// This feature is <b>disabled by default</b> and must be explicitly enabled — it is never for production.
    /// The setting is ignored unless the <c>OpenHoldemEngine</c> category is enabled at Trace level.
    /// </summary>
    public bool EnableSensitiveCardLogging { get; set; }
}
