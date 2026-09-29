namespace OpenHoldemEngine;

/// <summary>How bets and raises are sized in a game.</summary>
public enum BettingStructure : byte
{
    /// <summary>No-Limit: any bet or raise from the minimum up to the whole stack.</summary>
    NoLimit = 1,
}
