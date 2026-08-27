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
            : [];

        currentHand.TryGetBlindSeats(out var smallBlindSeatNo, out var bigBlindSeatNo);

        var playersRoundAmounts = currentHand.Actions
            .GroupBy(action => new { action.Street, action.SeatNo })
            .Select(group => new { group.Key.Street, group.Key.SeatNo, Amount = group.Max(action => action.Amount) })
            .ToList();

        return new GameData(
            GameId: game.Id,
            Options: new GameOptionData(game.MaxPlayersCount, game.InitialSmallBlind, game.InitialDealerSeatNo, game.AllowRebuy, game.MaxHandsCount, game.AutoIncrementHands),
            HandData: currentHand.No != default
                ? new HandData(
                    No: currentHand.No,
                    Seats: [.. game.Players.Select(player => new SeatData(
                            SeatNo: player.SeatNo,
                            Name: player.Name,
                            Hole: currentHand.GetSeatHoleCards(player.SeatNo),
                            LastAction: currentHand.Actions
                                .Where(action => action.SeatNo == player.SeatNo)
                                .OrderBy(action => action.No)
                                .Select(action => new PlayerActionData(action.Street, action.ActionType, action.Size, action.No))
                                .LastOrDefault(),
                            StartHandStack: currentHand.GetSeatStartStack(player.SeatNo),
                            Stack: currentHand.GetSeatCurrentStack(player.SeatNo),
                            HandResult: currentHand.PlayerEndStates
                                .Where(endState => endState.SeatNo == player.SeatNo)
                                .Select(endState => new HandResultData(
                                    Net: endState.WinAmount,
                                    Strength: endState.Strength,
                                    RankType: endState.RankType))
                                .SingleOrDefault()))],
                    DealerSeatNo: currentHand.DealerSeatNo,
                    SmallBlindSeatNo: smallBlindSeatNo,
                    BigBlindSeatNo: bigBlindSeatNo,
                    NextPlayerSeatNo: lastHandAction.NextSeatNo,
                    Street: lastHandAction.NextStreet,
                    StreetPot: playersRoundAmounts.Where(entry => entry.Street == lastHandAction.Street).Select(entry => entry.Amount).Sum(),
                    Pot: playersRoundAmounts.Where(entry => entry.Street != lastHandAction.NextStreet).Select(entry => entry.Amount).Sum(),
                    CommunityCards: [.. currentHand.GetDeckCards()],
                    Evaluations: evaluations)
                : default!);
    }

    /// <remarks>
    /// <see cref="PlayerActionEvaluationData.MinValue"/> and <see cref="PlayerActionEvaluationData.MaxValue"/>
    /// on a <see cref="PlayerActionType.Raise"/> are both "raise to" totals, the same absolute basis
    /// as <see cref="PlayerActionEvaluationData.MinValue"/>/<see cref="PlayerActionEvaluationData.MaxValue"/>
    /// on a <see cref="PlayerActionType.Call"/> are increments — see CLAUDE.md.
    /// </remarks>
    private static PlayerActionEvaluationData[] BuildEvaluations(Game game, byte nextSeatNo)
    {
        var table = HandTableFactory.Build(game);
        var window = BettingRules.LegalActions(table, nextSeatNo);
        var seat = table[nextSeatNo];

        var evaluations = new List<PlayerActionEvaluationData> { new(PlayerActionType.Fold) };

        var callValue = window.CallTo - seat.StreetWager;
        evaluations.Add(MoreThanZero(callValue)
            ? new PlayerActionEvaluationData(PlayerActionType.Call, callValue, callValue)
            : new PlayerActionEvaluationData(PlayerActionType.Check));

        if (window.MinRaiseTo is { } minRaiseTo && window.MaxRaiseTo is { } maxRaiseTo)
        {
            var minRaiseIncrement = minRaiseTo - seat.StreetWager;
            if (MoreThanZero(minRaiseIncrement) && MoreThanZero(maxRaiseTo) && maxRaiseTo >= minRaiseTo)
            {
                evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, minRaiseTo, maxRaiseTo));
                return [.. evaluations];
            }
        }

        // All-in is always allowed, even when the raise window above is degenerate. The bound is a
        // "raise to" total like every other raise bound, so it has to include what the seat already
        // has on the street — otherwise a seat that has posted a blind would be handed a size the
        // rules read as an under-raise and silently turn into a fold.
        var allInRaiseTo = seat.Stack + seat.StreetWager;
        evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, allInRaiseTo, allInRaiseTo));
        return [.. evaluations];
    }

    private static bool MoreThanZero(decimal amount) => amount > 0.001M;
}
