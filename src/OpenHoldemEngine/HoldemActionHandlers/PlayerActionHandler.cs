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
    private static readonly HandActionTypeToNPokerTypeConverter _handActionTypeConverter = new();

    internal async override Task ImplementHandleAsync(HoldemActionPlayerAction holdemAction, Game game)
    {
        var hand = game.GetCurrentHand();
        var nPokerState = _nPokerStateConverter.ConvertTo<NPokerState>(game);
        var nPokerPlayer = nPokerState.Table.Seats[nPokerState.NextPlayerIx]!;
        var nPokerPlayerAmount = (decimal)(nPokerPlayer.LastActionHistory?.Amount ?? 0);
        var gameData = _gameToGameContextConverter.ConvertTo<GameData>(game);

        await ShuffleDeckCardsIfNeededAsync();

        var (newState, messages) = game.GetEmulator()
            .ApplyAction(nPokerState, _handActionTypeConverter.ConvertTo<NPokerActionType>(holdemAction.ActionType),
                out var lastNPokerAction, (float)CalculateBetAmount());

        var handActionEntity = game.AddAction(
            seatNo: (byte)(nPokerState.NextPlayerIx + 1),
            street: gameData.HandData.Street,
            playerActionType: holdemAction.ActionType,
            size: holdemAction.Size,
            amount: (decimal)lastNPokerAction.Amount,
            addAmount: (decimal)lastNPokerAction.AddAmount,
            nextSeatNo: (byte)(newState.NextPlayerIx + 1),
            nextStreet: _streetToNPokerStreetConverter.ConvertFrom<Street>(newState.Street),
            paid: (decimal)lastNPokerAction.Paid);

        // if new deck cards appeared
        if (nPokerState.Street != newState.Street)
        {
            for (int ix = hand.DeckCards.Count; ix < newState.Table.CommunityCards.Count; ix++)
            {
                game.AddDeckCard(_cardToNCardConverter.ConvertFrom<Card>(newState.Table.CommunityCards.ElementAt(ix)));
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

            var handEndStates = hand.PlayerStartStates.Select(pss =>
            {
                var finishRoundStack = pss.StartStack;
                if (playersRoundBets.ContainsKey(pss.SeatNo))
                    finishRoundStack -= playersRoundBets[pss.SeatNo];
                finishRoundStack += nRoundResultMessage.PrizeMap.ContainsKey(pss.SeatNo - 1) ?
                    (decimal)nRoundResultMessage.PrizeMap[pss.SeatNo - 1] : 0;
                var hes = new HandPlayerEndState
                {
                    GameId = holdemAction.GameId,
                    HandNo = hand.No,
                    Hand = hand,
                    SeatNo = pss.SeatNo,
                    FinishStack = finishRoundStack,
                    WinAmount = finishRoundStack - pss.StartStack
                };
                return hes;
            });
            game.AddHandEndStates(handEndStates);
        }

        decimal CalculateBetAmount()
            => holdemAction.ActionType switch
            {
                PlayerActionType.Check => nPokerPlayerAmount,
                PlayerActionType.Call => nPokerPlayerAmount + gameData.HandData.GetEvalMinValue(PlayerActionType.Call),
                PlayerActionType.Fold => 0M,
                PlayerActionType.Raise => holdemAction.Size,
                PlayerActionType.Bet => holdemAction.Size + nPokerPlayerAmount,
                _ => throw new NotImplementedException()
            };

        async Task ShuffleDeckCardsIfNeededAsync()
        {
            if (HoldemEngineOptions.Instance.CardShuffler == default || (byte)hand.GetStreet() >= 5)
            {
                return;
            }
            var nextStreet = Enum.GetValues<Street>().Where(s => (byte)s > (byte)hand.GetStreet()).OrderBy(s => (byte)s).First();
            var deckCards = await HoldemEngineOptions.Instance.CardShuffler
                .GetDeckCardsAsync(holdemAction.GameId, hand.No);
            deckCards = deckCards.Except(hand.DeckCards.Select(dc => dc.Card));
            nPokerState = _nPokerStateConverter.ConvertToWithDeckCards(game, deckCards);
        }
    }
}
