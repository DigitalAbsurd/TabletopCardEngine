using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Engine;

namespace OpenHoldemEngine.Mappers;

/// <summary>Projects the persisted <see cref="Game"/> aggregate into the public <see cref="GameData"/> DTO.</summary>
internal static class GameDataMapper
{
    internal static GameData ToGameData(Game game)
    {
        var currentHand = game.GetCurrentHand();
        var lastHandAction = currentHand.GetLastAction();

        var evaluations = lastHandAction.NextSeatNo != 0
            ? BuildEvaluations(game, lastHandAction.NextSeatNo)
            : EquatableList<PlayerActionEvaluationData>.Empty;

        currentHand.TryGetBlindSeats(out var smallBlindSeatNo, out var bigBlindSeatNo);

        var playersRoundAmounts = currentHand.Actions
            .GroupBy(action => new { action.Street, action.SeatNo })
            .Select(group => new { group.Key.Street, group.Key.SeatNo, Amount = group.Max(action => action.Amount) })
            .ToList();

        return new GameData(
            GameId: game.Id,
            IsFinished: game.IsFinished,
            Version: game.Version,
            Options: new GameOptionData
            {
                GameType = game.GameType,
                BettingStructure = game.BettingStructure,
                MaxPlayersCount = game.MaxPlayersCount,
                InitialSmallBlind = game.InitialSmallBlind,
                InitialDealerSeatNo = game.InitialDealerSeatNo,
            },
            HandData: currentHand.No != default
                ? new HandData(
                    No: currentHand.No,
                    Seats: new EquatableList<SeatData>(game.Players.OrderBy(player => player.SeatNo).Select(player => new SeatData(
                            SeatNo: player.SeatNo,
                            Name: player.Name,
                            Hole: currentHand.GetSeatHoleCards(player.SeatNo) is { } hole ? new EquatableList<Card>(hole) : null,
                            LastAction: currentHand.Actions
                                .Where(action => action.SeatNo == player.SeatNo)
                                .OrderBy(action => action.No)
                                .Select(action => new PlayerActionData(action.Street, action.ActionType, action.Size, action.No))
                                .LastOrDefault(),
                            StartHandStack: currentHand.GetSeatStartStack(player.SeatNo),
                            BetSize: playersRoundAmounts
                                .Where(entry => entry.Street == lastHandAction.NextStreet && entry.SeatNo == player.SeatNo)
                                .Select(entry => (decimal?)entry.Amount)
                                .SingleOrDefault(),
                            Stack: currentHand.GetSeatCurrentStack(player.SeatNo),
                            HandResult: currentHand.PlayerEndStates
                                .Where(endState => endState.SeatNo == player.SeatNo)
                                .Select(endState => new HandResultData(
                                    Net: endState.WinAmount,
                                    Strength: endState.Strength,
                                    RankType: endState.RankType,
                                    Description: endState.Description))
                                .SingleOrDefault()))),
                    DealerSeatNo: currentHand.DealerSeatNo,
                    SmallBlindSeatNo: smallBlindSeatNo,
                    BigBlindSeatNo: bigBlindSeatNo,
                    NextPlayerSeatNo: lastHandAction.NextSeatNo,
                    Street: lastHandAction.NextStreet,
                    // Both pots split on NextStreet, the street on the clock. Splitting StreetPot on the
                    // last action's own Street instead counted a street that had just closed in both.
                    StreetPot: playersRoundAmounts.Where(entry => entry.Street == lastHandAction.NextStreet).Select(entry => entry.Amount).Sum(),
                    Pot: playersRoundAmounts.Where(entry => entry.Street != lastHandAction.NextStreet).Select(entry => entry.Amount).Sum(),
                    CommunityCards: new EquatableList<Card>(currentHand.GetDeckCards()),
                    Evaluations: evaluations)
                : null);
    }

    // BettingRules.Evaluate is the one answer to "what may this seat do": the same list validates
    // a Play request, so the caller is never offered an action the engine would then reject.
    private static EquatableList<PlayerActionEvaluationData> BuildEvaluations(Game game, byte nextSeatNo)
        => new(BettingRules.Evaluate(HandTableFactory.Build(game), nextSeatNo));
}
