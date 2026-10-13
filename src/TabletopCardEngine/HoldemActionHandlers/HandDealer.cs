using TabletopCardEngine.Data;
using TabletopCardEngine.Engine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.HoldemActionHandlers;

/// <summary>
/// The dealing both <c>StartGame</c> and <c>StartHand</c> perform once their own preconditions hold:
/// seat the players, build the deck, post the blinds and record the hole cards.
/// </summary>
internal static class HandDealer
{
    /// <summary>Seats <paramref name="joinSeats"/> and deals the next hand.</summary>
    /// <param name="game">The game to deal in.</param>
    /// <param name="joinSeats">
    /// Seats to fill. The first hand resolves the previous dealer by walking this list in order, so a
    /// caller dealing hand 1 has to pass it ascending by seat number.
    /// </param>
    /// <param name="cardShuffler">Supplies predetermined hole cards, or <see langword="null"/> to deal at random.</param>
    /// <param name="cancellationToken">Handed to the shuffler, which may do I/O.</param>
    internal static async Task DealAsync(Game game, IReadOnlyList<PlayerJoinData> joinSeats, ICardShuffler? cardShuffler, CancellationToken cancellationToken)
    {
        game.JoinPlayers(joinSeats);
        game.CreateNewHand(CalculateStartStacks());

        var table = HandTableFactory.Build(game, await BuildDeckAsync());
        table.DealerSeatNo = ResolvePreviousDealerSeatNo();

        HandRunner.StartHand(table);

        foreach (var seat in table.Seats)
        {
            game.AddHoleCards(seat.SeatNo, seat.Hole);
        }

        game.GetCurrentHand().DealerSeatNo = table.DealerSeatNo;

        // Amounts come off the table rather than from the nominal blinds, because a stack too short
        // to cover its blind posts what it has: `Size` stays the blind that was asked for, `Amount`
        // is what the seat actually put in, and all pot maths reads `Amount`.
        var smallBlindPost = table[table.SmallBlindSeatNo].StreetWager;
        var bigBlindPost = table[table.BigBlindSeatNo].StreetWager;

        game.AddAction(seatNo: table.SmallBlindSeatNo,
            street: Street.PreFlop,
            handActionType: HandActionType.SmallBlind,
            size: table.SmallBlind,
            amount: smallBlindPost,
            addAmount: smallBlindPost,
            nextSeatNo: table.BigBlindSeatNo,
            nextStreet: Street.PreFlop);

        game.AddAction(seatNo: table.BigBlindSeatNo,
            street: Street.PreFlop,
            handActionType: HandActionType.BigBlind,
            size: table.SmallBlind * 2,
            amount: bigBlindPost,
            addAmount: Math.Min(table.SmallBlind, bigBlindPost),
            nextSeatNo: table.NextSeatNo,
            nextStreet: Street.PreFlop);

        byte ResolvePreviousDealerSeatNo()
        {
            var previousHandNo = game.GetPreviousHand().No;
            var dealerPreviousHandSeatNo = game.GetPreviousHand().DealerSeatNo;
            if (previousHandNo == default)
            {
                var seatOrder = new LinkedList<byte>(joinSeats.Select(join => join.SeatNo));
                dealerPreviousHandSeatNo = seatOrder.Find(game.InitialDealerSeatNo)?.Previous?.Value ?? seatOrder.Last!.Value;
            }
            return dealerPreviousHandSeatNo;
        }

        async Task<CardDeck?> BuildDeckAsync()
        {
            if (cardShuffler == null)
            {
                return null;
            }

            var handNo = game.GetCurrentHand().No;
            var cards = new List<Card>();
            foreach (var seatNo in HandTableFactory.CurrentSeatNumbers(game))
            {
                cards.AddRange(await cardShuffler.GetHoleCardsAsync(game.Id, handNo, seatNo, cancellationToken));
            }
            return CardDeck.Predetermined(cards);
        }

        IEnumerable<HandPlayerStartState> CalculateStartStacks()
        {
            var handStartStates = game.GetCurrentHand().PlayerEndStates
                    .Select(endState => new HandPlayerStartState
                    {
                        GameId = game.Id,
                        HandNo = game.GetNextHandNo(),
                        SeatNo = endState.SeatNo,
                        StartStack = endState.FinishStack
                    })
                    .ToList();

            foreach (var joinData in joinSeats)
            {
                var startState = handStartStates.SingleOrDefault(state => state.SeatNo == joinData.SeatNo);
                if (startState == null)
                {
                    handStartStates.Add(new HandPlayerStartState
                    {
                        GameId = game.Id,
                        HandNo = game.GetNextHandNo(),
                        SeatNo = joinData.SeatNo,
                        StartStack = joinData.BuyInAmount
                    });
                }
                else
                {
                    startState.StartStack += joinData.BuyInAmount;
                }
            }
            return handStartStates;
        }
    }
}
