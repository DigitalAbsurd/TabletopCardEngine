using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// The dealing both <c>StartGame</c> and <c>StartHand</c> perform once their own preconditions hold:
/// seat the players, build the deck, post the antes and the blinds and record the hole cards.
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
        var previousHand = game.GetCurrentHand();
        game.CreateNewHand(CalculateStartStacks());
        SetBlindLevel(game.GetCurrentHand());

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

        // The antes are written apart from the blinds, so the per-street maximum the pot maths takes never sees
        // them: classic ones before the blind rows, the big blind ante right after its blind.
        var anteSeats = table.Seats.Where(seat => seat.Ante > 0).OrderBy(seat => seat.SeatNo).ToList();
        var bigBlindAnte = table.AnteType == AnteType.BigBlind && anteSeats.Count > 0;
        if (!bigBlindAnte)
        {
            AddAnteRows(table.SmallBlindSeatNo, Street.PreFlop);
        }

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
            nextSeatNo: bigBlindAnte ? table.BigBlindSeatNo : table.NextSeatNo,
            nextStreet: bigBlindAnte ? Street.PreFlop : table.Street);

        if (bigBlindAnte)
        {
            AddAnteRows(table.NextSeatNo, table.Street);
        }

        // A hand decided at the deal (the blinds leave nobody to ask) was already run out and awarded by
        // HandRunner.StartHand, so its board and end states are recorded now; otherwise both are empty.
        foreach (var card in table.Board)
        {
            game.AddDeckCard(card);
        }

        if (table.Prizes is { } prizes)
        {
            HandOutcomeRecorder.RecordResult(game, table, prizes);
        }

        // The blinds are fixed here, once the start stacks are known and before the table is built, because
        // ExcludeShortOfMoneySeats seats the blinds and PostBlind caps them at the stacks. They never change after the deal.
        void SetBlindLevel(Hand hand)
        {
            var previous = new BlindLevelState(previousHand.SmallBlind ?? game.InitialSmallBlind,
                previousHand.BlindLevel, previousHand.BlindLevelStartHandNo, previousHand.BlindThresholdsPassed);
            var state = BlindSchedule.Next(game.BlindSchedule, game.InitialSmallBlind, previous,
                previousHand.PlayerStartStates.CountDealtSeats(), hand.No, hand.PlayerStartStates.CountDealtSeats());

            hand.SmallBlind = state.SmallBlind;
            hand.BlindLevel = state.BlindLevel;
            hand.BlindLevelStartHandNo = state.BlindLevelStartHandNo;
            hand.BlindThresholdsPassed = state.BlindThresholdsPassed;

            // The ante keeps its ratio to the small blind as the blinds rise. This is the one place it is rounded.
            hand.Ante = game.Ante is { } ante && game.BlindSchedule is not null
                ? Math.Round(ante * state.SmallBlind / game.InitialSmallBlind, 2, MidpointRounding.AwayFromZero)
                : game.Ante;
        }

        // One row per ante posted. Size is the hand's nominal ante; Amount and AddAmount are what the seat posted,
        // less when its stack was short. The last row hands the clock on: to the small blind before the blinds, or
        // to whoever acts first after the big blind ante.
        void AddAnteRows(byte nextSeatNo, Street nextStreet)
        {
            for (var index = 0; index < anteSeats.Count; index++)
            {
                var seat = anteSeats[index];
                var last = index == anteSeats.Count - 1;
                game.AddAction(seatNo: seat.SeatNo,
                    street: Street.PreFlop,
                    handActionType: HandActionType.Ante,
                    size: table.Ante,
                    amount: seat.Ante,
                    addAmount: seat.Ante,
                    nextSeatNo: last ? nextSeatNo : anteSeats[index + 1].SeatNo,
                    nextStreet: last ? nextStreet : Street.PreFlop,
                    paid: seat.Ante);
            }
        }

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

            // The board follows the hole cards, because a hand decided at the deal runs it out inside
            // StartHand; StartHand draws only what it needs and the rest of the deck is discarded.
            cards.AddRange(await cardShuffler.GetDeckCardsAsync(game.Id, handNo, cancellationToken));
            return CardDeck.Predetermined(cards);
        }

        IEnumerable<HandPlayerStartState> CalculateStartStacks()
        {
            // A player who left after the current hand takes the stack along, so only the seats still
            // seated carry their finish stack into the next hand.
            var currentHand = game.GetCurrentHand();
            var handStartStates = currentHand.PlayerEndStates
                    .Where(endState => game.GetSeatPlayer(endState.SeatNo, currentHand.No) is { HandNoEnd: null })
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

            // Top-ups and rebuys made between the hands. The join rows of this hand were just written by
            // JoinPlayers and are already counted from joinSeats above, so only the other kinds are added here.
            foreach (var startState in handStartStates)
            {
                if (game.GetSeatPendingBuyIn(startState.SeatNo) is { } pending)
                {
                    startState.StartStack += pending.Amount;
                }
            }
            return handStartStates;
        }
    }
}
