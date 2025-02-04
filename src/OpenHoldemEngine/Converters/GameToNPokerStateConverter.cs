global using NPokerActionType = NPokerEngine.Types.ActionType;
global using NPokerDeck = NPokerEngine.Types.Deck;
global using NPokerEmulator = NPokerEngine.Emulator;
global using NPokerPlayer = NPokerEngine.Types.Player;
global using NPokerState = NPokerEngine.Types.GameState;
global using NPokerStreet = NPokerEngine.Types.StreetType;
global using NPokerTable = NPokerEngine.Types.Table;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Converters;
internal class GameToNPokerStateConverter : ValueConverter<Game, NPokerState>
{
    private static readonly StreetToNPokerStreetConverter _streetConverter = new StreetToNPokerStreetConverter();
    private static readonly CardToNCardConverter _cardToNCardConverter = new CardToNCardConverter();
    private static readonly HandActionTypeToNPokerTypeConverter _actionTypeConverter = new HandActionTypeToNPokerTypeConverter();

    public GameToNPokerStateConverter()
        : base(game => GameContextToNPokerState(game, null),
            nPokerState => null!)
    { }

    public NPokerState ConvertToWithDeckCards(Game game, IEnumerable<Card>? cards)
    {
        if (cards == null)
            return GameContextToNPokerState(game);

        var cheatCardsIds = cards.Select(c => _cardToNCardConverter.ConvertTo<NCard>(c).ToId()).ToList();
        var nDeck = new NPokerDeck(cheatCardIds: cheatCardsIds, cheat: true);
        return GameContextToNPokerState(game, nDeck);
    }

    private static NPokerState GameContextToNPokerState(Game game, NPokerDeck? nDeck = null)
    {
        var currentHand = game.GetCurrentHand();

        var deck = nDeck ?? new NPokerDeck();
        deck.Shuffle();

        var nState = new NPokerState()
        {
            Table = new NPokerTable(deck),
            RoundCount = currentHand.No,
            SmallBlindAmount = (float)game.InitialSmallBlind,
            Street = (NPokerStreet)_streetConverter.ConvertToProvider(currentHand.GetStreet())!,
            NextPlayerIx = currentHand.GetLastAction().NextSeatNo - 1,
        };

        currentHand.GetDeckCards()
            .ForEach(card => nState.Table.AddCommunityCard(_cardToNCardConverter.ConvertTo<NCard>(card)));

        foreach (var seat in Enumerable
            .Range(0, game.Players.Max(p => (byte?)p.SeatNo) ?? 0)
            .Select(ix => new { Ix = ix, No = (byte)(ix + 1) }))
        {
            var playerData = game.Players.SingleOrDefault(p => p.GetPlayerIx() == seat.Ix);
            var enginePlayer = playerData == null ? new NPokerPlayer($"Dummy#{seat.Ix}", 0) : new NPokerPlayer(playerData.Name, 0, playerData.Name);
            nState.Table.Seats.Sitdown(enginePlayer);

            if (playerData == null)
            {
                enginePlayer.PayInfo.UpdateToFold();
                continue;
            }

            var seatCards = currentHand.GetSeatHoleCards(seat.No);
            if (seatCards?.Any() == true)
                enginePlayer.AddHoleCards(seatCards
                    .Select(_cardToNCardConverter.ConvertTo<NCard>)
                    .ToArray());

            var startHandStack = currentHand.GetSeatStartStack(seat.No);
            foreach (var actionGroup in currentHand.GetSeatActions(seat.No))
            {
                foreach (var handAction in actionGroup.Value)
                {
                    enginePlayer.AddActionHistory(
                        kind: _actionTypeConverter.ConvertTo<NPokerActionType>(handAction.PlayAction),
                        chipAmount: (float)handAction.Amount,
                        addAmount: (float)handAction.AddAmount,
                        sbAmount: (float)game.InitialSmallBlind);
                }

                if (actionGroup.Key != currentHand.GetStreet())
                {
                    enginePlayer.RoundActionHistories[_streetConverter.ConvertTo<NPokerStreet>(actionGroup.Key)] = enginePlayer.ActionHistories.ToList();
                    enginePlayer.ActionHistories.Clear();
                }
            }

            enginePlayer.AppendChip((float)currentHand.GetSeatActualStack(seat.No));
            enginePlayer.PayInfo.UpdateByPay((float)currentHand.GetSeatBetAmount(seat.No));

            if (currentHand.IsSeatFolded(seat.No) ||
                (!currentHand.IsSeatDidActions(seat.No) && enginePlayer.Stack < float.Epsilon))
            {
                enginePlayer.PayInfo.UpdateToFold();
            }

            if (currentHand.IsSeatDidActions(seat.No) &&
                !currentHand.IsSeatFolded(seat.No) &&
                enginePlayer.Stack < float.Epsilon)
            {
                enginePlayer.PayInfo.UpdateToAllin();
            }
        }

        var dealerIx = currentHand.DealerSeatNo - 1;
        nState.Table.GetType()
            .GetField("_dealerButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(nState.Table, dealerIx);

        if (currentHand.TryGetBlindSeats(out var sbSeatNo, out var bbSeatNo))
        {
            nState.Table.SetBlindPositions(sbSeatNo - 1, bbSeatNo - 1);
        }

        return nState;
    }
}
