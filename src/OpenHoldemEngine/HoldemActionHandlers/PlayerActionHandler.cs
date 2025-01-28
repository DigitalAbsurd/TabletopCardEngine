using OpenHoldemEngine.Converters;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class PlayerActionHandler : HoldemActionHandler<HoldemActionPlayerAction>
{
    private static readonly GameToNPokerStateConverter _nPokerStateConverter = new();
    private static readonly GameToGameDataConverter _gameToGameContextConverter = new();
    private static readonly StreetToNPokerStreetConverter _streetToNPokerStreetConverter = new();
    private static readonly CardToNCardConverter _cardToNCardConverter = new();

    internal async override Task ImplementHandleAsync(HoldemActionPlayerAction holdemAction, HoldemDbContext dbContext)
    {
        var game = await dbContext.LoadGameWithCurrentHandAsync(holdemAction.GameId);
        var hand = game.GetCurrentHand();
        var nPokerState = _nPokerStateConverter.ConvertTo<(NPokerState nState, NPokerEmulator nEmulator)>(game);
        var nPokerPlayer = nPokerState.nState.Table.Seats[nPokerState.nState.NextPlayerIx]!;
        var nPokerPlayerAmount = (decimal)(nPokerPlayer.LastActionHistory?.Amount ?? 0);
        var gameData = _gameToGameContextConverter.ConvertTo<GameData>(game);

        var handActionEntity = new HandAction
        {
            GameId = holdemAction.GameId,
            HandNo = hand.No,
            No = hand.GetLastAction().No + 1,
            SeatNo = (byte)(nPokerState.nState.NextPlayerIx + 1),
            Size = holdemAction.Size,
            PlayAction = holdemAction.ActionType,
            Street = gameData.HandData.Street
        };

        var betAmount = holdemAction.ActionType switch
        {
            PlayerActionType.Check => nPokerPlayerAmount,
            PlayerActionType.Call => nPokerPlayerAmount + gameData.HandData.GetEvalMinValue(PlayerActionType.Call),
            PlayerActionType.Fold => 0M,
            PlayerActionType.Raise => holdemAction.Size,
            PlayerActionType.Bet => holdemAction.Size + nPokerPlayerAmount,
            _ => throw new NotImplementedException()
        };

        var nPokerActionType = holdemAction.ActionType switch
        {
            PlayerActionType.Check => NPokerActionType.CALL,
            PlayerActionType.Call => NPokerActionType.CALL,
            PlayerActionType.Fold => NPokerActionType.FOLD,
            PlayerActionType.Raise or PlayerActionType.Bet => NPokerActionType.RAISE,
            _ => throw new NotImplementedException()
        };

        if (HoldemEngineOptions.Instance.CardShuffler != default && (byte)hand.GetStreet() < 5)
        {
            var nextStreet = Enum.GetValues<Street>().Where(s => (byte)s > (byte)hand.GetStreet()).OrderBy(s => (byte)s).First();
            var deckCards = await HoldemEngineOptions.Instance.CardShuffler
                .GetDeckCardsAsync(holdemAction.GameId, hand.No);
            deckCards = deckCards.Except(hand.DeckCards.Select(dc => dc.Card));
            nPokerState = _nPokerStateConverter.ConvertToWithDeckCards(game, deckCards);
        }

        var (newState, messages) = nPokerState.nEmulator
            .ApplyAction(nPokerState.nState, nPokerActionType, out var lastNPokerAction, (float)betAmount);

        handActionEntity.NextStreet = _streetToNPokerStreetConverter.ConvertFrom<Street>(newState.Street);
        handActionEntity.NextSeatNo = (byte)(newState.NextPlayerIx + 1);
        handActionEntity.Amount = (decimal)lastNPokerAction.Amount;
        handActionEntity.AddAmount = (decimal)lastNPokerAction.AddAmount;
        handActionEntity.Paid = (decimal)lastNPokerAction.Paid;
        hand.Actions.Add(handActionEntity);
        dbContext.Entry(handActionEntity).State = Microsoft.EntityFrameworkCore.EntityState.Added;

        // if new deck cards appeared
        if (nPokerState.nState.Street != newState.Street)
        {
            for (int ix = hand.DeckCards.Count; ix < newState.Table.CommunityCards.Count; ix++)
            {
                var deckCard = new HandDeckCard
                {
                    GameId = holdemAction.GameId,
                    HandNo = hand.No,
                    Hand = hand,
                    Sequence = (byte)(ix + 1),
                    Card = _cardToNCardConverter.ConvertFrom<Card>(newState.Table.CommunityCards.ElementAt(ix))
                };
                dbContext.Entry(deckCard).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
        }

        // if hand ended
        var nRoundResultMessage = messages.OfType<NPokerEngine.Messages.RoundResultMessage>().SingleOrDefault();
        if (nRoundResultMessage != null)
        {
            var handBetGroups = hand.Actions
                .Concat([handActionEntity])
                .OrderBy(t => t.No)
                .GroupBy(t => new { t.SeatNo, t.Street });

            var playersRoundBets = new Dictionary<byte, decimal>();
            foreach (var betGroup in handBetGroups)
            {
                if (!playersRoundBets.ContainsKey(betGroup.Key.SeatNo))
                    playersRoundBets[betGroup.Key.SeatNo] = 0;

                if (!betGroup.Any()) continue;

                playersRoundBets[betGroup.Key.SeatNo] += betGroup.Max(g => g.Amount);
            }

            foreach (var pss in hand.PlayerStartStates)
            {
                var finishRoundStack = pss.StartStack;
                if (playersRoundBets.ContainsKey(pss.SeatNo))
                    finishRoundStack -= playersRoundBets[pss.SeatNo];
                finishRoundStack += nRoundResultMessage.PrizeMap.ContainsKey(pss.SeatNo - 1) ? (decimal)nRoundResultMessage.PrizeMap[pss.SeatNo - 1] : 0;
                var hes = new HandPlayerEndState
                {
                    GameId = holdemAction.GameId,
                    HandNo = hand.No,
                    Hand = hand,
                    SeatNo = pss.SeatNo,
                    FinishStack = finishRoundStack,
                    WinAmount = finishRoundStack - pss.StartStack
                };
                hand.PlayerEndStates.Add(hes);
                dbContext.Entry(hes).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
