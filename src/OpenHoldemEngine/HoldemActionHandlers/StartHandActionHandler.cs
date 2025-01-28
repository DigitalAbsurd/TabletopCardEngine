using OpenHoldemEngine.Converters;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class StartHandActionHandler : HoldemActionHandler<HoldemActionStartHand>
{
    private static readonly GameToNPokerStateConverter _nPokerStateConverter = new();
    private static readonly CardToNCardConverter _cardToNCardConverter = new();

    internal override async Task ImplementHandleAsync(HoldemActionStartHand startHandData, HoldemDbContext dbContext)
    {
        var game = await dbContext.LoadGameWithCurrentHandAsync(startHandData.GameId);

        var nextHandNo = game.GetCurrentHand().No + 1;
        var dealerPreviousHandSeatNo = game.GetCurrentHand().No == default ?
            GetPreviousDealerSeatNo() :
            game.GetCurrentHand().DealerSeatNo;

        game.Players.AddRange(startHandData.JoinSeats.Select(
            seat => new Player
            {
                Name = seat.Name,
                SeatNo = seat.SeatNo,
                RoundNoStart = nextHandNo
            }));

        game.BuyIns.AddRange(startHandData.JoinSeats.
            Select(b => new BuyIn
            {
                HandNo = nextHandNo,
                SeatNo = b.SeatNo,
                Amount = b.BuyInAmount
            }));

        //var handStartStates = game.GetCurrentHand().PlayerEndStates
        //        .Select(t => new HandPlayerStartState
        //        {
        //            GameId = game.Id,
        //            HandNo = nextHandNo,
        //            SeatNo = t.SeatNo,
        //            StartStack = t.FinishStack + startHandData.JoinSeats.SingleOrDefault(b => b.SeatNo == t.SeatNo)?.BuyInAmount ?? 0
        //        })
        //        .Union(startHandData.JoinSeats
        //            .Where(b => !game.GetCurrentHand().PlayerEndStates.Any(hes => hes.SeatNo == b.SeatNo))
        //            .Select(b => new HandPlayerStartState
        //            {
        //                GameId = game.Id,
        //                HandNo = nextHandNo,
        //                SeatNo = b.SeatNo,
        //                StartStack = b.BuyInAmount
        //            }))
        //        .Where(hps => !startHandData.KickOffSeats.Contains(hps.SeatNo))
        //        .ToList();

        var handStartStates = game.GetCurrentHand().PlayerEndStates
                .Select(t => new HandPlayerStartState
                {
                    GameId = game.Id,
                    HandNo = nextHandNo,
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
                    HandNo = nextHandNo,
                    SeatNo = joinData.SeatNo,
                    StartStack = joinData.BuyInAmount
                });
            }
            else
            {
                hss.StartStack += joinData.BuyInAmount;
            }
        }


        game.Hands.Add(new Hand
        {
            No = nextHandNo,
            GameId = game.Id,
            PlayerStartStates = handStartStates
        });

        var nPokerState = _nPokerStateConverter.ConvertTo<(NPokerState nState, NPokerEmulator nEmulator)>(game);

        if (HoldemEngineOptions.Instance.CardShuffler != default)
            nPokerState = _nPokerStateConverter.ConvertToWithDeckCards(game, await GetCustomShuffledHoleCards());

        ShiftDealerButtonToPreviousHand();

        var (handNState, messages) = nPokerState.nEmulator.StartNewRound(nPokerState.nState);

        for (int ix = 0; ix < handNState.Table.Seats.Players.Count; ix++)
        {
            var nPlayer = handNState.Table.Seats.Players[ix];
            if (nPlayer.HoleCards == null || !nPlayer.HoleCards.Any())
                continue;
            if (game.Players.SingleOrDefault(t => t.GetPlayerIx() == ix) == null)
                continue;

            for (int ih = 0; ih < nPlayer.HoleCards.Count; ih++)
            {
                game.GetCurrentHand().HoleCards.Add(
                    new HandHoleCard
                    {
                        Card = _cardToNCardConverter.ConvertFrom<Card>(nPlayer.HoleCards[ih]),
                        HandNo = nextHandNo,
                        SeatNo = (byte)(ix + 1),
                        Sequence = (byte)(ih + 1)
                    });
            }
        }

        game.GetCurrentHand().DealerSeatNo = (byte)(handNState.Table.DealerButton + 1);
        var nPlayerSmallBlindSeatNo = (byte)(handNState.Table.Seats.Players.IndexOf(
            handNState.Table.Seats.Players.Single(t => t.LastActionHistory?.ActionType == NPokerActionType.SMALL_BLIND)) + 1);
        var nPlayerBigBlindSeatNo = (byte)(handNState.Table.Seats.Players.IndexOf(
            handNState.Table.Seats.Players.Single(t => t.LastActionHistory?.ActionType == NPokerActionType.BIG_BLIND)) + 1);

        game.GetCurrentHand().Actions.AddRange(new[]
        {
            new HandAction
            {
                No = 1,
                HandNo = nextHandNo,
                PlayAction = PlayerActionType.SmallBlind,
                SeatNo = nPlayerSmallBlindSeatNo,
                Size = game.InitialSmallBlind,
                Amount = game.InitialSmallBlind,
                AddAmount = game.InitialSmallBlind,
                NextSeatNo = nPlayerBigBlindSeatNo
            },
            new HandAction
            {
                No = 2,
                HandNo = nextHandNo,
                PlayAction = PlayerActionType.BigBlind,
                SeatNo = nPlayerBigBlindSeatNo,
                Size = game.InitialSmallBlind * 2,
                Amount = game.InitialSmallBlind * 2,
                AddAmount = game.InitialSmallBlind,
                NextSeatNo = (byte)(handNState.NextPlayerIx + 1)
            },
        });

        await dbContext.SaveChangesAsync();

        byte GetPreviousDealerSeatNo()
        {
            var ll = new LinkedList<byte>(startHandData.JoinSeats.Select(t => t.SeatNo));
            return ll.Find(game.InitialDealerSeatNo)?.Previous?.Value ?? ll.Last!.Value;
        }

        void ShiftDealerButtonToPreviousHand()
        {
            nPokerState.nState.Table.GetType()
                .GetField("_dealerButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(nPokerState.nState.Table, dealerPreviousHandSeatNo - 1);
            //for (int ix = 0; ix < nPokerState.nState.Table.Seats.Players.Count; ix++)
            //{
            //    if (nPokerState.nState.Table.DealerButton != dealerPreviousHandSeatNo - 1)
            //        nPokerState.nState.Table.ShiftDealerButton();

            //    if (nPokerState.nState.Table.DealerButton == dealerPreviousHandSeatNo - 1)
            //        break;
            //}
        }

        async Task<IEnumerable<Card>> GetCustomShuffledHoleCards()
        {
            var shuffler = HoldemEngineOptions.Instance.CardShuffler!;
            var playerCards = new List<Card>();

            for (int ix = 0; ix < nPokerState.nState.Table.Seats.Players.Count; ix++)
            {
                playerCards.AddRange(await shuffler.GetHoleCardsAsync(game.Id, nextHandNo, (byte)(ix + 1)));
            }
            return playerCards;
        }
    }
}
