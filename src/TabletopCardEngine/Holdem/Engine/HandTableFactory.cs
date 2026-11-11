using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Holdem.Engine;

/// <summary>
/// Rehydrates a <see cref="HandTable"/> from the persisted <see cref="Game"/> and its current
/// <see cref="Hand"/>. Called fresh on every request — nothing here is held between calls.
/// </summary>
internal static class HandTableFactory
{
    /// <summary>
    /// Builds the table. When <paramref name="deck"/> is omitted, a random deck is shuffled that
    /// excludes every card already dealt (hole cards and board) — without that exclusion, a
    /// random rebuild could hand out a card that is already on the table or in someone's hand.
    /// </summary>
    internal static HandTable Build(Game game, CardDeck? deck = null)
    {
        var hand = game.GetCurrentHand();
        var seats = CurrentSeatPlayers(game).Select(player => BuildSeat(hand, player)).ToList();

        hand.TryGetBlindSeats(out var smallBlindSeatNo, out var bigBlindSeatNo);

        var table = new HandTable
        {
            SmallBlind = hand.SmallBlind ?? game.InitialSmallBlind,
            Ante = hand.Ante ?? 0M,
            AnteType = game.AnteType,
            BettingStructure = game.BettingStructure,
            RaiseCap = game.RaiseCap,
            Deck = deck ?? CardDeck.Shuffled(exclude: hand.GetDeckCards().Concat(seats.SelectMany(seat => seat.Hole))),
            Seats = seats,
            Street = hand.GetStreet(),
            DealerSeatNo = hand.DealerSeatNo,
            SmallBlindSeatNo = smallBlindSeatNo,
            BigBlindSeatNo = bigBlindSeatNo,
            NextSeatNo = hand.GetLastAction().NextSeatNo,
            NextBetOrder = hand.Actions.Select(action => action.No + 1).DefaultIfEmpty(0).Max(),
        };
        table.Board.AddRange(hand.GetDeckCards());

        return table;
    }

    /// <summary>The seats a hand is actually being played at, ascending by seat number, one per seat.</summary>
    internal static IEnumerable<byte> CurrentSeatNumbers(Game game)
        => CurrentSeatPlayers(game).Select(player => player.SeatNo);

    // The current hand's start states say who it is dealt to: a player who left after an earlier hand has
    // none, and a newly dealt hand gets them before the table is built, so one rule serves both rehydrating
    // a hand in progress and dealing a new one.
    private static IEnumerable<Data.Player> CurrentSeatPlayers(Game game)
        => game.GetHandPlayers(game.GetCurrentHand());

    private static TableSeat BuildSeat(Hand hand, Data.Player player)
    {
        var seatNo = player.SeatNo;
        var seat = new TableSeat { SeatNo = seatNo, Name = player.Name };
        seat.Hole.AddRange(hand.GetSeatHoleCards(seatNo) ?? []);
        seat.Stack = hand.GetSeatActualStack(seatNo);
        seat.Committed = hand.GetSeatBetAmount(seatNo);
        seat.Ante = hand.GetSeatAnte(seatNo);

        if (hand.GetSeatActions(seatNo).TryGetValue(hand.GetStreet(), out var streetActions))
        {
            foreach (var action in streetActions)
            {
                seat.Record(action.ActionType, action.Amount, action.AddAmount, action.Paid, action.No);
            }
        }

        // The antes and blind posts are the first rows of a dealt hand, so any action means the deal has happened.
        // A seat without hole cards then was sat out at the deal (HandRunner.DealHoleCards); before the deal every
        // seat lacks them. An ante row counts as having acted, so a seat its ante put all in rehydrates all in.
        var satOut = hand.Actions.Count > 0 && seat.Hole.Count == 0;
        var folded = satOut || hand.IsSeatFolded(seatNo) || (!hand.HasSeatActed(seatNo) && seat.Stack <= 0);
        var allIn = !folded && hand.HasSeatActed(seatNo) && seat.Stack <= 0;
        seat.Status = folded ? SeatStatus.Folded : allIn ? SeatStatus.AllIn : SeatStatus.Waiting;

        return seat;
    }
}
