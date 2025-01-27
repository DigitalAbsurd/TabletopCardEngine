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
        var nPokerState = _gameToNPokerStateConverter.ConvertTo<(NPokerState nState, NPokerEmulator nEmulator)>(game);
        var evaluations = Array.Empty<PlayerActionEvaluationData>();

        if (nPokerState.nState.NextPlayerIx >= 0)
        {
            var nPossibleActions = nPokerState.nEmulator.GeneratePossibleActions(nPokerState.nState);
            evaluations = _nPossibleActionsToActionsData.ConvertTo<PlayerActionEvaluationData[]>(nPossibleActions);
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
                                StartRoundStack: currentHand.GetSeatStartStack(t.SeatNo),
                                Stack: currentHand.GetSeatCurrentStack(t.SeatNo)))
                            .ToArray(),
                    DealerSeatNo: currentHand.DealerSeatNo,
                    SmallBlindSeatNo: sbSeatNo,
                    BigBlindSeatNo: bbSeatNo,
                    NextPlayerSeatNo: (byte)(nPokerState.nState.NextPlayerIx + 1),
                    Street: currentHand.GetLastAction().Street,
                    StreetPot: currentHand.GetCurrentStreetPot(),
                    Pot: currentHand.GetPot(),
                    CommunityCards: currentHand.GetDeckCards().ToArray(),
                    Evaluations: evaluations) :
                default(HandData)!);
    }
}
