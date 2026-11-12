using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// <c>StartTournament</c>: draws the seats, opens every table as an ordinary <see cref="Game"/> and deals its first
/// hand. The games are created and dealt by the same handlers <see cref="HoldemTable"/> runs, on the tournament's own
/// <see cref="HoldemDbContext"/>, so they are saved in the tournament's transaction; going through
/// <see cref="IHoldemTable"/> would open a context of its own, and the table guard would refuse it anyway.
/// </summary>
internal static class TournamentStarter
{
    /// <summary>One table opened by the start, for the log.</summary>
    internal readonly record struct OpenedTable(int TableNo, Game Game);

    internal static async Task<(HoldemError? Error, IReadOnlyList<OpenedTable> Tables)> StartAsync(
        TournamentActionStart action, Tournament tournament, HoldemDbContext dbContext, HoldemOptions options, CancellationToken cancellationToken)
    {
        if (tournament.State != (byte)TournamentState.Registering)
        {
            return (new HoldemError(HoldemErrorCode.TournamentAlreadyStarted, PackResource.HoldemValidation_TournamentAlreadyStarted), []);
        }

        var entries = tournament.ActiveEntries().ToList();
        if (entries.Count < tournament.MinEntries)
        {
            return (new HoldemError(HoldemErrorCode.NotEnoughPlayers, PackResource.HoldemValidation_TournamentNotEnoughEntries), []);
        }

        var seed = action.SeatDrawSeed ?? Random.Shared.Next();
        tournament.SeatDrawSeed = seed;
        tournament.StartedAt = options.TimeProvider.GetUtcNow();
        tournament.State = (byte)TournamentState.Running;

        var draw = DrawSeats(entries, tournament.MaxPlayersPerTable, seed);
        var level = TournamentClock.DealLevelAt(tournament.Levels, TournamentClock.LevelIndex(tournament.Levels, tournament.StartedAt.Value, tournament.StartedAt.Value));

        var opened = new List<OpenedTable>(draw.Count);
        for (var index = 0; index < draw.Count; index++)
        {
            var (seated, buttonSeatNo) = draw[index];
            var tableNo = index + 1;

            var game = new Game();
            var create = HoldemAction.CreateGame(new GameOptionData
            {
                GameType = GameType.Holdem,
                BettingStructure = tournament.BettingStructure,
                TableFormat = tournament.TableFormat,
                MaxPlayersCount = tournament.MaxPlayersPerTable,
                RaiseCap = tournament.RaiseCap,
                InitialSmallBlind = tournament.Levels[0].SmallBlind,
                Ante = tournament.Levels[0].Ante > 0 ? tournament.Levels[0].Ante : null,
                AnteType = tournament.AnteType,
                InitialDealerSeatNo = buttonSeatNo,
            }, Guid.NewGuid());
            var error = await new CreateGameActionHandler(options.Settings).ImplementHandleAsync(create, game, cancellationToken);
            if (error is not null)
            {
                return (error, []);
            }

            dbContext.Add(game);
            tournament.Tables.Add(new TournamentTable { TournamentId = tournament.Id, TableNo = tableNo, GameId = game.Id });

            var joins = new PlayerJoinData[seated.Count];
            for (var seat = 0; seat < seated.Count; seat++)
            {
                var seatNo = (byte)(seat + 1);
                seated[seat].TableNo = tableNo;
                seated[seat].SeatNo = seatNo;
                joins[seat] = new PlayerJoinData(seated[seat].Name, seatNo, tournament.StartingStack);
            }

            // A table drawn with one player keeps its game and its seat, but no hand: it waits for a player, like a
            // table left with one player later on.
            if (joins.Length >= 2)
            {
                error = await new StartGameActionHandler(options.CardShuffler, level)
                    .ImplementHandleAsync(new HoldemActionStartGame(game.Id, joins), game, cancellationToken);
                if (error is not null)
                {
                    return (error, []);
                }
            }

            opened.Add(new OpenedTable(tableNo, game));
        }

        return (null, opened);
    }

    /// <summary>
    /// The seat draw. The order of the draws is part of the contract, because one seed has to give one result for the
    /// same entries: the entries, ascending by entry number, are shuffled first with one Fisher-Yates pass from the
    /// last position down, each step taking <c>random.Next(i + 1)</c>; then each table's button is drawn in table order
    /// with <c>random.Next(1, size + 1)</c>. There are <c>T = ceil(N / maxPlayersPerTable)</c> tables, the first
    /// <c>N mod T</c> of them one player larger, and each takes the next players of the shuffled order into seats
    /// <c>1</c> up.
    /// </summary>
    internal static IReadOnlyList<(IReadOnlyList<TournamentEntry> Seated, byte ButtonSeatNo)> DrawSeats(
        IReadOnlyList<TournamentEntry> entries, int maxPlayersPerTable, int seed)
    {
        var random = new Random(seed);
        var shuffled = entries.OrderBy(entry => entry.EntryNo).ToArray();
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var other = random.Next(index + 1);
            (shuffled[index], shuffled[other]) = (shuffled[other], shuffled[index]);
        }

        var tableCount = (shuffled.Length + maxPlayersPerTable - 1) / maxPlayersPerTable;
        var baseSize = shuffled.Length / tableCount;
        var largerTables = shuffled.Length % tableCount;

        var tables = new List<(IReadOnlyList<TournamentEntry>, byte)>(tableCount);
        var next = 0;
        for (var table = 0; table < tableCount; table++)
        {
            var size = baseSize + (table < largerTables ? 1 : 0);
            var buttonSeatNo = (byte)random.Next(1, size + 1);
            tables.Add((shuffled[next..(next + size)], buttonSeatNo));
            next += size;
        }

        return tables;
    }
}
