using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Engine;

/// <summary>
/// The rules' view of one hand in progress: the seats, the board, the deck, and whose turn it
/// is. Collapses NPokerEngine's <c>GameState</c> + <c>Table</c> + <c>Seats</c>. Rebuilt fresh
/// from the database on every request by <see cref="HandTableFactory"/> and mutated in place by
/// <see cref="HandRunner"/> — nothing here needs to survive between calls, so there is no
/// cloning and no defensive copying.
/// </summary>
internal sealed class HandTable
{
    public required decimal SmallBlind { get; init; }
    public required CardDeck Deck { get; init; }

    /// <summary>Occupied seats only, ascending by <see cref="TableSeat.SeatNo"/>. No empty-seat padding.</summary>
    public required IReadOnlyList<TableSeat> Seats { get; init; }

    public Street Street { get; set; }
    public byte DealerSeatNo { get; set; }
    public byte SmallBlindSeatNo { get; set; }
    public byte BigBlindSeatNo { get; set; }

    /// <summary>1-based seat on the clock; 0 means nobody is (hand over, or nobody seated).</summary>
    public byte NextSeatNo { get; set; }

    public List<Card> Board { get; } = [];

    /// <summary>Set once the pot has been awarded; null while the hand is still being played.</summary>
    public IReadOnlyDictionary<byte, decimal>? Prizes { get; set; }

    public TableSeat? Find(byte seatNo) => Seats.FirstOrDefault(seat => seat.SeatNo == seatNo);

    public TableSeat this[byte seatNo]
        => Find(seatNo) ?? throw new KeyNotFoundException($"No seat {seatNo} at this table.");

    public int ActiveCount => Seats.Count(seat => seat.IsActive);
    public int WaitingCount => Seats.Count(seat => seat.IsWaiting);

    /// <summary>
    /// Walks the seating ring starting strictly after <paramref name="after"/>, wrapping once,
    /// and returns the first seat matching <paramref name="match"/> — or 0 if none do. Examines
    /// every seat exactly once, mirroring NPokerEngine's <c>Table.FindEntitledPlayerPosition</c>.
    /// </summary>
    public byte NextSeatAfter(byte after, Func<TableSeat, bool> match) => Walk(after, offset: 1, match);

    /// <summary>Same walk, but <paramref name="from"/> itself is the first candidate.</summary>
    public byte NextSeatFrom(byte from, Func<TableSeat, bool> match) => Walk(from, offset: 0, match);

    private byte Walk(byte anchor, int offset, Func<TableSeat, bool> match)
    {
        var ordered = Seats.OrderBy(seat => seat.SeatNo).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var anchorIndex = ordered.FindIndex(seat => seat.SeatNo == anchor);
        var startIndex = anchorIndex < 0 ? 0 : anchorIndex;

        for (var step = offset; step < offset + ordered.Count; step++)
        {
            var seat = ordered[(startIndex + step) % ordered.Count];
            if (match(seat))
            {
                return seat.SeatNo;
            }
        }
        return 0;
    }
}
