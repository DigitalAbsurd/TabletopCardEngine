using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Engine;

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

        hand.TryGetBlindSeats(out var sbSeatNo, out var bbSeatNo);

        var table = new HandTable
        {
            SmallBlind = game.InitialSmallBlind,
            Deck = deck ?? CardDeck.Shuffled(exclude: hand.GetDeckCards().Concat(seats.SelectMany(s => s.Hole))),
            Seats = seats,
            Street = hand.GetStreet(),
            DealerSeatNo = hand.DealerSeatNo,
            SmallBlindSeatNo = sbSeatNo,
            BigBlindSeatNo = bbSeatNo,
            NextSeatNo = hand.GetLastAction().NextSeatNo,
        };
        table.Board.AddRange(hand.GetDeckCards());

        return table;
    }

    /// <summary>The seats a hand is actually being played at, ascending by seat number, one per seat.</summary>
    internal static IEnumerable<byte> CurrentSeatNumbers(Game game)
        => CurrentSeatPlayers(game).Select(player => player.SeatNo);

    private static IEnumerable<Data.Player> CurrentSeatPlayers(Game game)
        => game.Players
            .GroupBy(player => player.SeatNo)
            .Select(seatPlayers => seatPlayers.OrderByDescending(player => player.RoundNoStart).First())
            .OrderBy(player => player.SeatNo);

    private static TableSeat BuildSeat(Hand hand, Data.Player player)
    {
        var seatNo = player.SeatNo;
        var seat = new TableSeat { SeatNo = seatNo, Name = player.Name };
        seat.Hole.AddRange(hand.GetSeatHoleCards(seatNo) ?? []);
        seat.Stack = hand.GetSeatActualStack(seatNo);
        seat.Committed = hand.GetSeatBetAmount(seatNo);

        if (hand.GetSeatActions(seatNo).TryGetValue(hand.GetStreet(), out var streetActions))
        {
            foreach (var action in streetActions)
            {
                seat.Record(action.PlayAction, action.Amount, action.AddAmount, action.Paid);
            }
        }

        var folded = hand.IsSeatFolded(seatNo) || (!hand.IsSeatDidActions(seatNo) && seat.Stack <= 0);
        var allIn = !folded && hand.IsSeatDidActions(seatNo) && seat.Stack <= 0;
        seat.Status = folded ? SeatStatus.Folded : allIn ? SeatStatus.AllIn : SeatStatus.Waiting;

        return seat;
    }
}
