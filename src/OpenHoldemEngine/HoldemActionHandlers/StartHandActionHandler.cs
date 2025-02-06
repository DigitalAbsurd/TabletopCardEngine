using FluentResults;
using OpenHoldemEngine.Converters;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class StartHandActionHandler : HoldemActionHandler<HoldemActionStartHand>
{
    private static readonly GameToNPokerStateConverter _nPokerStateConverter = new();
    private static readonly CardToNCardConverter _cardToNCardConverter = new();

    internal override async Task<Result> ImplementHandleAsync(HoldemActionStartHand startHandData, Game game)
    {
        if (!CheckIsPreviousHandEnded())
        {
            return Result.Fail(PackResource.HoldemValidation_PreviousHandNotEnded);
        }

        game.JoinPlayers(startHandData.JoinSeats);

        game.CreateNewHand(CalculateStartStacks());

        var nPokerState = _nPokerStateConverter.ConvertToWithDeckCards(game, await GetShuffledHoleCards());

        ShiftDealerButtonToPreviousHand();

        var (handNState, messages) = game.GetEmulator().StartNewRound(nPokerState);

        handNState.Table.Seats.Players
            .Select((p, ix) => new
            {
                NPlayer = p,
                SeatNo = (byte)(ix + 1),
                HoleCards = p.HoleCards.Select(_cardToNCardConverter.ConvertFrom<Card>)
            })
            .Where(t => game.Players.Any(gp => gp.SeatNo == t.SeatNo))
            .ToList()
            .ForEach(t => game.AddHoleCards(t.SeatNo, t.HoleCards));

        game.GetCurrentHand().DealerSeatNo = (byte)(handNState.Table.DealerButton + 1);

        game.AddAction(seatNo: (byte)(handNState.Table.SmallBlindPosition! + 1),
            street: Street.PreFlop,
            playerActionType: PlayerActionType.SmallBlind,
            size: game.InitialSmallBlind,
            amount: game.InitialSmallBlind,
            addAmount: game.InitialSmallBlind,
            nextSeatNo: (byte)(handNState.Table.BigBlindPosition! + 1),
            nextStreet: Street.PreFlop);

        game.AddAction(seatNo: (byte)(handNState.Table.BigBlindPosition! + 1),
            street: Street.PreFlop,
            playerActionType: PlayerActionType.BigBlind,
            size: game.InitialSmallBlind * 2,
            amount: game.InitialSmallBlind * 2,
            addAmount: game.InitialSmallBlind,
            nextSeatNo: (byte)(handNState.NextPlayerIx + 1),
            nextStreet: Street.PreFlop);

        return Result.Ok();

        void ShiftDealerButtonToPreviousHand()
        {
            var previousHandNo = game.GetPreviousHand().No;

            var dealerPreviousHandSeatNo = game.GetPreviousHand().DealerSeatNo;
            if (previousHandNo == default)
            {
                var ll = new LinkedList<byte>(startHandData.JoinSeats.Select(t => t.SeatNo));
                dealerPreviousHandSeatNo = ll.Find(game.InitialDealerSeatNo)?.Previous?.Value ?? ll.Last!.Value;
            }

            nPokerState.Table.GetType()
                .GetField("_dealerButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(nPokerState.Table, dealerPreviousHandSeatNo - 1);
        }

        bool CheckIsPreviousHandEnded()
            => game.GetCurrentHand().No == 0 || game.GetCurrentHand().PlayerEndStates.Any();

        async Task<IEnumerable<Card>?> GetShuffledHoleCards()
        {
            var shuffler = HoldemEngineOptions.Instance.CardShuffler;
            if (shuffler == null) return null;

            var playerCards = new List<Card>();
            var handNo = game.GetCurrentHand().No;

            for (int ix = 0; ix < game.Players.Count; ix++)
            {
                playerCards.AddRange(await shuffler.GetHoleCardsAsync(game.Id, handNo, (byte)(ix + 1)));
            }
            return playerCards;
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
