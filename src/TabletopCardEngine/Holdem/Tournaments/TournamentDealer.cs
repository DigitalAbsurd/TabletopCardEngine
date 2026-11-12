using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// <c>DealTable</c>: between two hands of one table, records the players the ended hand eliminated, finishes the
/// tournament when one player is left, and otherwise deals the table's next hand at the clock's level. Like
/// <see cref="TournamentStarter"/>, it runs the game's own handlers on the tournament's <see cref="HoldemDbContext"/>, so
/// the tournament and the table's game are saved together. It never writes a stack: a player leaves the game through
/// <see cref="EntityExtensions.LeaveSeat"/>, and the chips stay where the ledger put them.
/// </summary>
internal static class TournamentDealer
{
    /// <summary>What a successful call did, for the log.</summary>
    /// <param name="Eliminated">The entries the call placed, best place first.</param>
    /// <param name="Winner">The entry that won, when the call finished the tournament.</param>
    /// <param name="Dealt">The table whose next hand the call dealt, if it dealt one.</param>
    internal sealed record Outcome(IReadOnlyList<TournamentEntry> Eliminated, TournamentEntry? Winner, TournamentStarter.OpenedTable? Dealt)
    {
        internal static readonly Outcome None = new([], null, null);
    }

    internal static async Task<(HoldemError? Error, Outcome Outcome)> DealAsync(
        TournamentActionDealTable action, Tournament tournament, HoldemDbContext dbContext, HoldemOptions options, CancellationToken cancellationToken)
    {
        switch ((TournamentState)tournament.State)
        {
            case TournamentState.Registering:
                return (new HoldemError(HoldemErrorCode.TournamentNotStarted, PackResource.HoldemValidation_TournamentNotStarted), Outcome.None);
            case TournamentState.Finished:
                return (new HoldemError(HoldemErrorCode.TournamentFinished, PackResource.HoldemValidation_TournamentFinished), Outcome.None);
        }

        var table = tournament.Tables.SingleOrDefault(candidate => candidate.TableNo == action.TableNo);
        if (table is null || table.IsBroken)
        {
            return (InvalidTable(), Outcome.None);
        }

        var game = await dbContext.LoadGameWithCurrentHandAsync(table.GameId);
        if (game.Id == default)
        {
            // The loader attaches a placeholder for a missing game; it must not be saved.
            dbContext.Remove(game);
            return (InvalidTable(), Outcome.None);
        }

        if (!game.IsCurrentHandEnded())
        {
            return (new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded), Outcome.None);
        }

        var hand = game.GetCurrentHand();
        List<TournamentEntry> eliminated = hand.No == 0 ? [] : Eliminate(tournament, table.TableNo, game, hand);
        var gameChanged = eliminated.Count > 0;

        var remaining = Remaining(tournament).ToList();
        if (remaining.Count == 1)
        {
            var error = await FinishAsync(tournament, remaining[0], dbContext, game, options, cancellationToken);
            return error is null ? (null, new Outcome(eliminated, remaining[0], null)) : (error, Outcome.None);
        }

        var seated = remaining
            .Where(entry => entry.TableNo == table.TableNo && entry.SeatNo is not null && entry.CarriedStack is null)
            .OrderBy(entry => entry.SeatNo)
            .ToList();

        TournamentStarter.OpenedTable? dealt = null;
        if (seated.Count >= 2)
        {
            var now = options.TimeProvider.GetUtcNow();
            var level = TournamentClock.DealLevelAt(tournament.Levels, TournamentClock.LevelIndex(tournament.Levels, tournament.StartedAt!.Value, now));

            // A table drawn with a single player has no hand yet, and StartHand refuses to deal before hand 1.
            var error = hand.No == 0
                ? await new StartGameActionHandler(options.CardShuffler, level).ImplementHandleAsync(
                    new HoldemActionStartGame(game.Id, [.. seated.Select(entry => new PlayerJoinData(entry.Name, entry.SeatNo!.Value, tournament.StartingStack))]),
                    game, cancellationToken)
                : await new StartHandActionHandler(options.CardShuffler, level).ImplementHandleAsync(new HoldemActionStartHand(game.Id), game, cancellationToken);
            if (error is not null)
            {
                return (error, Outcome.None);
            }

            gameChanged = true;
            dealt = new TournamentStarter.OpenedTable(table.TableNo, game);
        }

        if (gameChanged)
        {
            game.Version++;
        }

        return (null, new Outcome(eliminated, null, dealt));
    }

    /// <summary>The entries still playing: not withdrawn and not placed.</summary>
    private static IEnumerable<TournamentEntry> Remaining(Tournament tournament)
        => tournament.ActiveEntries().Where(entry => entry.Place is null);

    /// <summary>
    /// Places every player seated at the table who ended <paramref name="hand"/> without chips, and frees its seat. The
    /// places are counted from the players left before this call, so busts recorded earlier, at any table, keep the
    /// worse places.
    /// </summary>
    private static List<TournamentEntry> Eliminate(Tournament tournament, int tableNo, Game game, Hand hand)
    {
        var remaining = Remaining(tournament).ToList();
        var busted = remaining
            .Where(entry => entry.TableNo == tableNo && entry.SeatNo is { } seatNo && entry.CarriedStack is null
                && hand.PlayerEndStates.Any(endState => endState.SeatNo == seatNo)
                && hand.GetSeatFinishStack(seatNo) < BlindSchedule.DecimalMinPrecision)
            .ToList();
        if (busted.Count == 0)
        {
            return busted;
        }

        var prizes = PrizeTable(tournament);
        var groups = busted
            .GroupBy(entry => hand.GetSeatStartStack(entry.SeatNo!.Value))
            .OrderByDescending(group => group.Key);

        var placed = new List<TournamentEntry>(busted.Count);
        var place = remaining.Count - busted.Count + 1;
        foreach (var group in groups)
        {
            var tied = group.OrderBy(entry => entry.EntryNo).ToList();
            var shares = SplitPrize(Enumerable.Range(place, tied.Count).Sum(covered => PrizeOf(prizes, covered)), tied.Count);
            for (var index = 0; index < tied.Count; index++)
            {
                var entry = tied[index];
                entry.Place = place;
                entry.Prize = shares[index];
                entry.EliminatedInHandNo = hand.No;
                entry.EliminatedAtTableNo = tableNo;
                if (game.GetSeatedPlayer(entry.SeatNo!.Value) is { } player)
                {
                    game.LeaveSeat(player);
                }

                entry.TableNo = null;
                entry.SeatNo = null;
                placed.Add(entry);
            }

            place += tied.Count;
        }

        return placed;
    }

    /// <summary>
    /// Ends the tournament with <paramref name="winner"/> in first place, and finishes every table's game that is not
    /// finished yet through <c>FinishGame</c>'s own handler.
    /// </summary>
    private static async Task<HoldemError?> FinishAsync(Tournament tournament, TournamentEntry winner, HoldemDbContext dbContext, Game dealtGame,
        HoldemOptions options, CancellationToken cancellationToken)
    {
        winner.Place = 1;
        winner.Prize = PrizeOf(PrizeTable(tournament), 1);
        winner.TableNo = null;
        winner.SeatNo = null;
        tournament.State = (byte)TournamentState.Finished;
        tournament.FinishedAt = options.TimeProvider.GetUtcNow();

        foreach (var table in tournament.Tables.OrderBy(table => table.TableNo))
        {
            var game = table.GameId == dealtGame.Id ? dealtGame : await dbContext.LoadGameWithCurrentHandAsync(table.GameId);
            if (game.IsFinished)
            {
                continue;
            }

            var error = await new FinishGameActionHandler().ImplementHandleAsync(new HoldemActionFinishGame(game.Id), game, cancellationToken);
            if (error is not null)
            {
                return error;
            }

            game.Version++;
        }

        return null;
    }

    /// <summary>
    /// The prize of every paid place, first place first: place <c>k</c> wins <c>PrizePool × Payouts[k - 1] / 100</c>
    /// rounded to the cent, and first place what the others leave of the pool, so that the prizes add up to it exactly.
    /// Each share is rounded as it is computed, never carried unrounded (see the pot splits in
    /// <see cref="PotDistribution"/> for why).
    /// </summary>
    internal static IReadOnlyList<decimal> PrizeTable(decimal prizePool, IReadOnlyList<decimal> payouts)
    {
        var prizes = new decimal[payouts.Count];
        for (var index = 1; index < payouts.Count; index++)
        {
            prizes[index] = Math.Round(prizePool * payouts[index] / 100, 2, MidpointRounding.AwayFromZero);
        }

        prizes[0] = prizePool - prizes.Skip(1).Sum();
        return prizes;
    }

    /// <summary>
    /// Splits <paramref name="total"/> into <paramref name="count"/> equal shares rounded down to the cent, the cents
    /// left over going one each to the first shares: the caller orders them by entry number.
    /// </summary>
    internal static decimal[] SplitPrize(decimal total, int count)
    {
        var share = Math.Round(total / count, 2, MidpointRounding.ToZero);
        var shares = Enumerable.Repeat(share, count).ToArray();
        var leftover = total - (share * count);
        for (var index = 0; leftover >= 0.01m; index++)
        {
            shares[index] += 0.01m;
            leftover -= 0.01m;
        }

        return shares;
    }

    private static IReadOnlyList<decimal> PrizeTable(Tournament tournament)
        => PrizeTable(tournament.BuyIn * tournament.ActiveEntries().Count(), tournament.Payouts);

    private static decimal PrizeOf(IReadOnlyList<decimal> prizes, int place)
        => place <= prizes.Count ? prizes[place - 1] : 0;

    private static HoldemError InvalidTable()
        => new(HoldemErrorCode.InvalidTable, PackResource.HoldemValidation_TournamentInvalidTable);
}
