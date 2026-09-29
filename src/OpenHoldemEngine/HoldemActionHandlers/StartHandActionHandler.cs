using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Engine;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class StartHandActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionStartHand>
{
    internal override async Task<Result> ImplementHandleAsync(HoldemActionStartHand startHandData, Game game)
    {
        if (!game.IsCurrentHandEnded())
        {
            return Result.Fail(PackResource.HoldemValidation_PreviousHandNotEnded);
        }

        game.JoinPlayers(startHandData.JoinSeats);
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
            playerActionType: PlayerActionType.SmallBlind,
            size: table.SmallBlind,
            amount: smallBlindPost,
            addAmount: smallBlindPost,
            nextSeatNo: table.BigBlindSeatNo,
            nextStreet: Street.PreFlop);

        game.AddAction(seatNo: table.BigBlindSeatNo,
            street: Street.PreFlop,
            playerActionType: PlayerActionType.BigBlind,
            size: table.SmallBlind * 2,
            amount: bigBlindPost,
            addAmount: Math.Min(table.SmallBlind, bigBlindPost),
            nextSeatNo: table.NextSeatNo,
            nextStreet: Street.PreFlop);

        return Result.Ok();

        byte ResolvePreviousDealerSeatNo()
        {
            var previousHandNo = game.GetPreviousHand().No;
            var dealerPreviousHandSeatNo = game.GetPreviousHand().DealerSeatNo;
            if (previousHandNo == default)
            {
                var seatOrder = new LinkedList<byte>(startHandData.JoinSeats.Select(join => join.SeatNo));
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
                cards.AddRange(await cardShuffler.GetHoleCardsAsync(game.Id, handNo, seatNo));
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

            foreach (var joinData in startHandData.JoinSeats)
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
