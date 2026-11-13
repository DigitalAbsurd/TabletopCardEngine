using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Engine;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The multi-table part of <c>DealTable</c> (ADR-0011, point 8). The table being dealt breaks when the players left fit
/// at fewer tables, and otherwise gives its next big blind to the shortest table while it is two players larger. Only
/// the table being dealt ever breaks or gives a player away, and a move never touches the target's game: the player is a
/// tournament row carrying its stack until the target deals, so play at the other tables goes on undisturbed.
/// </summary>
internal static class TournamentBalancer
{
    /// <summary>One player taken off a table, for the log.</summary>
    internal readonly record struct Move(int EntryNo, int FromTableNo, int ToTableNo, byte SeatNo, decimal Stack);

    /// <summary>
    /// Breaks <paramref name="table"/> or balances it, between two of its hands and after the ended hand's eliminations.
    /// A table's count is its seated players plus the players moving to it. With <c>P</c> players left, <c>S</c> seats a
    /// table and <c>T</c> tables not broken, the table breaks when <c>T &gt; ceil(P / S)</c>: every seated player
    /// moves, in ascending seat order, and then every player moving to it. Otherwise, while its count is at least the lowest count of the other tables plus
    /// two, the player due for its next big blind moves. Every move goes to the table with the lowest count (ties: the
    /// lowest table number), into its lowest seat neither held nor reserved.
    /// </summary>
    /// <returns>Whether the table broke; the caller then finishes its game and deals nothing.</returns>
    internal static bool BreakOrBalance(Tournament tournament, TournamentTable table, Game game, List<Move> moves)
    {
        var remaining = TournamentDealer.Remaining(tournament).ToList();
        var others = tournament.Tables.Where(other => !other.IsBroken && other.TableNo != table.TableNo).OrderBy(other => other.TableNo).ToList();
        var tablesNeeded = (remaining.Count + tournament.MaxPlayersPerTable - 1) / tournament.MaxPlayersPerTable;
        if (others.Count + 1 > tablesNeeded)
        {
            foreach (var entry in SeatedAt(remaining, table.TableNo).ToList())
            {
                moves.Add(MoveOut(tournament, remaining, others, table, game, entry));
            }

            // A player already moving to the table keeps its carried stack and only changes the reservation.
            foreach (var entry in MovingTo(remaining, table.TableNo).ToList())
            {
                moves.Add(MoveOut(tournament, remaining, others, table, game, entry));
            }

            table.IsBroken = true;
            return true;
        }

        while (others.Count > 0 && Count(remaining, table.TableNo) >= others.Min(other => Count(remaining, other.TableNo)) + 2)
        {
            var seated = SeatedAt(remaining, table.TableNo).ToList();
            var bigBlindSeatNo = NextBigBlindSeatNo(tournament, game, seated);
            if (seated.SingleOrDefault(entry => entry.SeatNo == bigBlindSeatNo) is not { } entry)
            {
                // Fewer than two seated players have chips, so there is no next hand and no big blind to give away.
                break;
            }

            moves.Add(MoveOut(tournament, remaining, others, table, game, entry));
        }

        return false;
    }

    /// <summary>The players seated at the table, not moving, ascending by seat.</summary>
    internal static IEnumerable<TournamentEntry> SeatedAt(IEnumerable<TournamentEntry> remaining, int tableNo)
        => remaining.Where(entry => entry.TableNo == tableNo && entry.SeatNo is not null && entry.CarriedStack is null).OrderBy(entry => entry.SeatNo);

    /// <summary>The players moving to the table, ascending by the seat reserved for them there.</summary>
    internal static IEnumerable<TournamentEntry> MovingTo(IEnumerable<TournamentEntry> remaining, int tableNo)
        => remaining.Where(entry => entry.TableNo == tableNo && entry.SeatNo is not null && entry.CarriedStack is not null).OrderBy(entry => entry.SeatNo);

    /// <summary>A table's count: its seated players plus the players moving to it, whose seat there is reserved.</summary>
    private static int Count(IEnumerable<TournamentEntry> remaining, int tableNo)
        => remaining.Count(entry => entry.TableNo == tableNo);

    /// <summary>
    /// Takes <paramref name="entry"/> off its seat at <paramref name="from"/> with the stack it ended the last hand
    /// with (or off its reservation there, keeping the stack it carries), and reserves it a seat at the target. The game at <paramref name="from"/> loses the player after its current
    /// hand, exactly as <c>LeaveSeat</c> has it; the target's game is not loaded.
    /// </summary>
    private static Move MoveOut(Tournament tournament, List<TournamentEntry> remaining, List<TournamentTable> targets, TournamentTable from, Game game,
        TournamentEntry entry)
    {
        var fromSeatNo = entry.SeatNo!.Value;
        decimal stack;
        if (entry.CarriedStack is { } carried)
        {
            // Moving to <from>: the player has no seat in its game yet, so there is nothing to leave.
            stack = carried;
        }
        else
        {
            stack = StackOf(tournament, game, fromSeatNo);
            if (game.GetSeatedPlayer(fromSeatNo) is { } player)
            {
                game.LeaveSeat(player);
            }
        }

        var target = targets.OrderBy(candidate => Count(remaining, candidate.TableNo)).ThenBy(candidate => candidate.TableNo).First();
        var taken = remaining.Where(other => other.TableNo == target.TableNo).Select(other => other.SeatNo).ToHashSet();
        var seatNo = Enumerable.Range(1, tournament.MaxPlayersPerTable).Select(seat => (byte)seat).FirstOrDefault(seat => !taken.Contains(seat));
        if (seatNo == 0)
        {
            // The break condition leaves room at the other tables, and a balance only moves to a table two players shorter.
            throw new InvalidOperationException($"Table {target.TableNo} has no free seat for a moving player.");
        }

        entry.TableNo = target.TableNo;
        entry.SeatNo = seatNo;
        entry.CarriedStack = stack;
        return new Move(entry.EntryNo, from.TableNo, target.TableNo, seatNo, stack);
    }

    /// <summary>
    /// What a seated player holds between two hands: the ended hand's finish stack, or the starting stack at a table
    /// that has dealt no hand yet. Every seated player was dealt into the ended hand, because a player moving in is
    /// seated only by the deal that deals it in.
    /// </summary>
    internal static decimal StackOf(Tournament tournament, Game game, byte seatNo)
    {
        var hand = game.GetCurrentHand();
        return hand.No == 0 ? tournament.StartingStack : hand.GetSeatFinishStack(seatNo);
    }

    /// <summary>
    /// The seat the engine would make big blind in the table's next hand, among the players seated there: the button
    /// moves on from the ended hand's, as <c>StartHand</c> moves it, and before hand 1 from the seat before the game's
    /// first button, as <c>StartGame</c> resolves it.
    /// </summary>
    private static byte NextBigBlindSeatNo(Tournament tournament, Game game, IReadOnlyList<TournamentEntry> seated)
    {
        var seats = seated
            .Select(entry => new TableSeat { SeatNo = entry.SeatNo!.Value, Name = entry.Name, Stack = StackOf(tournament, game, entry.SeatNo!.Value) })
            .ToList();
        var hand = game.GetCurrentHand();
        var firstButtonIndex = seats.FindIndex(seat => seat.SeatNo == game.InitialDealerSeatNo);
        var dealerSeatNo = hand.No != 0 ? hand.DealerSeatNo
            : seats.Count == 0 ? (byte)0
            : seats[firstButtonIndex > 0 ? firstButtonIndex - 1 : seats.Count - 1].SeatNo;

        var table = new HandTable
        {
            SmallBlind = game.InitialSmallBlind,
            Deck = CardDeck.Predetermined([]),
            Seats = seats,
            DealerSeatNo = dealerSeatNo,
        };
        return HandRunner.NextBigBlindSeatNo(table);
    }
}
