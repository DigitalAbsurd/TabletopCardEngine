namespace TabletopCardEngine.Holdem.Options;

/// <summary>Validation limits applied to every game the engine runs.</summary>
public sealed class HoldemSettings
{
    /// <summary>Fewest seats a hand may be dealt to. Within 2 to 10; <c>WithSettings</c> throws outside that range.</summary>
    public int GamePlayersMinCount { get; set; } = 2;

    /// <summary>
    /// Most seats a hand may be dealt to. Within 2 to 10, and 10 by default; <c>WithSettings</c> throws outside
    /// that range. A game's table format caps its seats as well, so the default 6-Max table still stops at 6.
    /// </summary>
    public int GamePlayersMaxCount { get; set; } = 10;

    /// <summary>
    /// When <c>true</c>, the engine logs every seat's hole cards and the board at Trace level.
    /// This feature is <b>disabled by default</b> and must be explicitly enabled — it is never for production.
    /// The setting is ignored unless the <c>TabletopCardEngine</c> category is enabled at Trace level.
    /// </summary>
    public bool EnableSensitiveCardLogging { get; set; }
}
