namespace TabletopCardEngine.Holdem;

/// <summary>How bets and raises are sized in a game.</summary>
public enum BettingStructure : byte
{
    /// <summary>No-Limit: any bet or raise from the minimum up to the whole stack.</summary>
    NoLimit = 1,

    /// <summary>Pot-Limit: any bet or raise from the minimum up to the size of the pot after a call.</summary>
    PotLimit = 2,

    /// <summary>
    /// Fixed-Limit: bets and raises of one fixed size: the big blind pre-flop and on the flop, two big blinds on the
    /// turn and river, at most <see cref="Dto.GameOptionData.RaiseCap"/> bets a street.
    /// </summary>
    FixedLimit = 3,
}
