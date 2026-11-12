namespace TabletopCardEngine.Holdem.Engine;

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

    /// <summary>Total wagered this hand, across every street and the ante included — the side-pot input.</summary>
    public decimal Committed { get; set; }

    /// <summary>
    /// The ante this seat posted. It is dead money: part of <see cref="Committed"/>, never a street bet, so it
    /// counts towards neither calling nor raising.
    /// </summary>
    public decimal Ante { get; set; }

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
                if (StreetBets[i].Action != HandActionType.Fold)
                {
                    return StreetBets[i].Amount;
                }
            }
            return 0M;
        }
    }

    public bool HasActedThisStreet => StreetBets.Count > 0;

    /// <summary>
    /// True only preflop, and only until this seat acts again: a blind post is not a voluntary action, so a blind
    /// gets an option even when nobody has raised over it. That is the big blind's option, and it is the small
    /// blind's too when the big blind posted no more than it (a big blind its ante or its stack left short), which
    /// leaves the small blind holding the standing wager.
    /// </summary>
    public bool BlindOptionPending => StreetBets is [{ Action: HandActionType.BigBlind or HandActionType.SmallBlind }];

    internal void Record(HandActionType action, decimal amount, decimal addAmount, decimal paid, int order)
        => StreetBets.Add(new SeatBet(action, amount, addAmount, paid, order));

    internal void ClearStreet() => StreetBets.Clear();
}
