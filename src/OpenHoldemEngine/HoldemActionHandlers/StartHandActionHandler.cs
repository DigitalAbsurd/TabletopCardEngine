using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Engine;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class StartHandActionHandler : HoldemActionHandler<HoldemActionStartHand>
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

        game.AddAction(seatNo: table.SmallBlindSeatNo,
            street: Street.PreFlop,
            playerActionType: PlayerActionType.SmallBlind,
            size: table.SmallBlind,
            amount: table.SmallBlind,
            addAmount: table.SmallBlind,
            nextSeatNo: table.BigBlindSeatNo,
            nextStreet: Street.PreFlop);

        game.AddAction(seatNo: table.BigBlindSeatNo,
            street: Street.PreFlop,
            playerActionType: PlayerActionType.BigBlind,
            size: table.SmallBlind * 2,
            amount: table.SmallBlind * 2,
            addAmount: table.SmallBlind,
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
            var shuffler = HoldemEngineOptions.Instance.CardShuffler;
            if (shuffler == null)
            {
                return null;
            }

            var handNo = game.GetCurrentHand().No;
            var cards = new List<Card>();
            foreach (var seatNo in HandTableFactory.CurrentSeatNumbers(game))
            {
                cards.AddRange(await shuffler.GetHoleCardsAsync(game.Id, handNo, seatNo));
            }
            return CardDeck.Predetermined(cards);
        }

        IEnumerable<HandPlayerStartState> CalculateStartStacks()
        {
            var handStartStates = game.GetCurrentHand().PlayerEndStates
                    .Select(t => new HandPlayerStartState
                    {
                        GameId = game.Id,
                        HandNo = game.GetNextHandNo(),
                        SeatNo = t.SeatNo,
                        StartStack = t.FinishStack
                    })
                    .ToList();

            foreach (var joinData in startHandData.JoinSeats)
            {
                var hss = handStartStates.SingleOrDefault(hss => hss.SeatNo == joinData.SeatNo);
                if (hss == null)
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
                    hss.StartStack += joinData.BuyInAmount;
                }
            }
            return handStartStates;
        }
    }
}
