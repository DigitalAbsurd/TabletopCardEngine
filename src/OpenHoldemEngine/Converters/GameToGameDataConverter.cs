using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.Converters;
internal class GameToGameDataConverter : ValueConverter<Game, GameData>
{
    private static readonly NPossibleActionsToActionsDataConverter _nPossibleActionsToActionsData = new NPossibleActionsToActionsDataConverter();
    private static readonly GameToNPokerStateConverter _gameToNPokerStateConverter = new GameToNPokerStateConverter();
    private static readonly HandActionTypeToNPokerTypeConverter _actionTypeToNPokerTypeConverter = new HandActionTypeToNPokerTypeConverter();

    public GameToGameDataConverter()
        : base(game => GameToGameContext(game),
            gameData => null!)
    { }

    private static GameData GameToGameContext(Game game)
    {
        var nPokerState = _gameToNPokerStateConverter.ConvertTo<NPokerState>(game);
        var evaluations = new List<PlayerActionEvaluationData>();

        if (nPokerState.NextPlayerIx >= 0)
        {
            var enginePlayer = nPokerState.Table.Seats[nPokerState.NextPlayerIx];
            var playerCurrentStreetAmount = enginePlayer.ActionHistories?.LastOrDefault()?.Amount ?? 0;
            var nPossibleActions = game.GetEmulator().GeneratePossibleActions(nPokerState);
            foreach (var nPossibleAction in nPossibleActions)
            {
                switch (nPossibleAction.possibleAction)
                {
                    case NPokerActionType.FOLD:
                        evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Fold));
                        break;
                    case NPokerActionType.CALL:
                        var callValue = (decimal)(nPossibleAction.amountInterval.Value - playerCurrentStreetAmount);
                        if (MoreThanZero(callValue))
                        {
                            evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Call, callValue, callValue));
                        }
                        else
                        {
                            evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Check));
                        }
                        break;
                    case NPokerActionType.RAISE:
                        var maxRaiseValue = (decimal)(nPossibleAction.amountInterval.MaxValue ?? 0 - playerCurrentStreetAmount);
                        var minRaiseValue = (decimal)(nPossibleAction.amountInterval.Value - playerCurrentStreetAmount);
                        if (MoreThanZero(minRaiseValue) && MoreThanZero(maxRaiseValue) && maxRaiseValue >= minRaiseValue)
                        {
                            evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, minRaiseValue, maxRaiseValue));
                        }
                        else
                        {
                            //AllIn always allowed
                            var allInValue = (decimal)(enginePlayer.Stack);
                            evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, allInValue, allInValue));
                        }
                        break;
                    default:
                        throw new NotImplementedException($"{nPossibleAction.possibleAction}");
                }
            }
            //evaluations = _nPossibleActionsToActionsData.ConvertTo<PlayerActionEvaluationData[]>(nPossibleActions);
        }

        var currentHand = game.GetCurrentHand();
        currentHand.TryGetBlindSeats(out var sbSeatNo, out var bbSeatNo);

        return new GameData(
            GameId: game.Id,
            Options: new GameOptionData(game.MaxPlayersCount, game.InitialSmallBlind, game.InitialDealerSeatNo, game.AllowRebuy, game.MaxHandsCount, game.AutoIncrementHands),
            HandData: currentHand.No != default ?
                new HandData(
                    No: currentHand.No,
                    Seats: game.Players.Select(t => new SeatData(
                                SeatNo: t.SeatNo,
                                Name: t.Name,
                                Hole: currentHand.GetSeatHoleCards(t.SeatNo),
                                LastAction: currentHand.Actions
                                    .Where(act => act.SeatNo == t.SeatNo)
                                    .OrderBy(act => act.No)
                                    .Select(act => new PlayerActionData(act.Street, act.PlayAction, act.Size, act.No))
                                    .LastOrDefault(),
                                StartRoundStack: currentHand.GetSeatStartStack(t.SeatNo),
                                Stack: currentHand.GetSeatCurrentStack(t.SeatNo)))
                            .ToArray(),
                    DealerSeatNo: currentHand.DealerSeatNo,
                    SmallBlindSeatNo: sbSeatNo,
                    BigBlindSeatNo: bbSeatNo,
                    NextPlayerSeatNo: (byte)(nPokerState.NextPlayerIx + 1),
                    Street: currentHand.GetLastAction().NextStreet,
                    StreetPot: currentHand.GetCurrentStreetPot(),
                    Pot: currentHand.GetPot(),
                    CommunityCards: currentHand.GetDeckCards().ToArray(),
                    Evaluations: evaluations.ToArray()) :
                default(HandData)!);

        bool MoreThanZero(decimal amount)
            => amount > 0.001M;
    }
}
