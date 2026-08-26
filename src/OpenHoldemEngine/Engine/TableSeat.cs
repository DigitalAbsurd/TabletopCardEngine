using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Engine;

/// <summary>
/// One occupied seat's rules-relevant state for the hand in progress. Collapses NPokerEngine's
/// <c>Player</c> + <c>PayInfo</c> + its per-street action-history list into scalars: nothing here
/// reads a history entry by anything but "the last one" or "the biggest one this street", so the
/// history itself does not need to be kept once those two facts are captured.
/// </summary>
internal sealed class TableSeat
{
    public required byte SeatNo { get; init; }
    public required string Name { get; init; }
    public decimal Stack { get; set; }

    /// <summary>Total wagered this hand, across every street — the side-pot input.</summary>
    public decimal Committed { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Waiting;
    public List<Card> Hole { get; } = [];

    /// <summary>This street's actions only; cleared when the street closes.</summary>
    public List<SeatBet> StreetBets { get; } = [];

    public bool IsActive => Status != SeatStatus.Folded;
    public bool IsWaiting => Status == SeatStatus.Waiting;

    /// <summary>The seat's cumulative wager for the street — its last non-fold action's amount.</summary>
    public decimal StreetWager
    {
        get
        {
            for (var i = StreetBets.Count - 1; i >= 0; i--)
            {
                if (StreetBets[i].Action != PlayerActionType.Fold)
                {
                    return StreetBets[i].Amount;
                }
            }
            return 0M;
        }
    }

    public bool HasActedThisStreet => StreetBets.Count > 0;

    /// <summary>
    /// True only preflop, and only until this seat acts again: the big blind gets an option even
    /// when nobody has raised over it.
    /// </summary>
    public bool BigBlindOptionPending => StreetBets is [{ Action: PlayerActionType.BigBlind }];

    internal void Record(PlayerActionType action, decimal amount, decimal addAmount, decimal paid)
        => StreetBets.Add(new SeatBet(action, amount, addAmount, paid));

    internal void ClearStreet() => StreetBets.Clear();
}
