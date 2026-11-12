using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests.Persistence.Upgrades;

/// <summary>
/// <c>AddTournaments</c> adds the <c>Tournament</c>, <c>TournamentEntry</c> and <c>TournamentTable</c> tables and changes
/// no existing one. A heads-up game whose only hand has ended must come through it intact, and the three new tables
/// must exist and be empty.
/// </summary>
internal sealed class AddTournamentsUpgradeFixture : IUpgradeFixture
{
    // The rows StartGame and a small-blind fold write for this game at schema 6: the hand has
    // ended, so it has end states. Seats are (seat, name, buy-in, hole cards, finish stack, net); the
    // rows of the ledger are (seat, next seat, street, next street, action type, size, amount, add amount).
    private static readonly SeededGame[] _games =
    [
        new(Guid.Parse("4c8d1e7a-2b5f-4a96-8d3c-7e0f1a2b3c4d"), MaxPlayersCount: 2, SmallBlind: 50M, DealerSeatNo: 1,
            Seats: [(1, "Alice", 10_000M, "kd", "8s", 9_950M, -50M, 1_863_680), (2, "Bob", 10_000M, "jd", "ks", 10_050M, 50M, 1_875_968)],
            Actions:
            [
                (1, 2, "PreFlop", "PreFlop", "SmallBlind", 50M, 50M, 50M),
                (2, 1, "PreFlop", "PreFlop", "BigBlind", 100M, 100M, 50M),
                (1, 0, "PreFlop", "Finished", "Fold", 0M, 0M, 0M),
            ]),
    ];

    public string MigrationName => "AddTournaments";

    public async Task SeedAsync(DbContext previousSchema, TestDatabaseKind kind)
    {
        foreach (var game in _games)
        {
            var id = GuidLiteral(game.Id, kind);
            var statements = new List<string>
            {
                $"""INSERT INTO {Table("Game", kind)} ("Id", "InitialSmallBlind", "MaxPlayersCount", "InitialDealerSeatNo", "GameType", "BettingStructure", "TableFormat", "RaiseCap", "Ante", "AnteType", "IsFinished", "Version") VALUES ({id}, {Number(game.SmallBlind)}, {game.MaxPlayersCount}, {game.DealerSeatNo}, 'Holdem', 'NoLimit', 'HeadsUp', 4, NULL, 'Classic', {FalseLiteral(kind)}, 3)""",
                $"""INSERT INTO {Table("Hand", kind)} ("GameId", "No", "DealerSeatNo") VALUES ({id}, 1, {game.DealerSeatNo})""",
            };

            foreach (var (seatNo, name, buyIn, first, second, finishStack, net, strength) in game.Seats)
            {
                statements.Add($"""INSERT INTO {Table("Player", kind)} ("GameId", "SeatNo", "HandNoStart", "Name", "HandNoEnd") VALUES ({id}, {seatNo}, 1, '{name}', NULL)""");
                statements.Add($"""INSERT INTO {Table("BuyIn", kind)} ("GameId", "SeatNo", "HandNo", "Amount") VALUES ({id}, {seatNo}, 1, {Number(buyIn)})""");
                statements.Add($"""INSERT INTO {Table("HandPlayerStartState", kind)} ("GameId", "HandNo", "SeatNo", "StartStack") VALUES ({id}, 1, {seatNo}, {Number(buyIn)})""");
                statements.Add($"""INSERT INTO {Table("HandPlayerEndState", kind)} ("GameId", "HandNo", "SeatNo", "FinishStack", "WinAmount", "Strength", "RankType", "Description") VALUES ({id}, 1, {seatNo}, {Number(finishStack)}, {Number(net)}, {strength}, 'HighCard', NULL)""");
                statements.Add($"""INSERT INTO {Table("HandHoleCard", kind)} ("GameId", "HandNo", "SeatNo", "Sequence", "Card") VALUES ({id}, 1, {seatNo}, 1, '{first}'), ({id}, 1, {seatNo}, 2, '{second}')""");
            }

            var actionNo = 0;
            foreach (var (seatNo, nextSeatNo, street, nextStreet, actionType, size, amount, addAmount) in game.Actions)
            {
                statements.Add($"""INSERT INTO {Table("HandAction", kind)} ("GameId", "HandNo", "No", "SeatNo", "NextSeatNo", "Street", "NextStreet", "ActionType", "Size", "Amount", "AddAmount", "Paid") VALUES ({id}, 1, {++actionNo}, {seatNo}, {nextSeatNo}, '{street}', '{nextStreet}', '{actionType}', {Number(size)}, {Number(amount)}, {Number(addAmount)}, 0)""");
            }

            foreach (var statement in statements)
            {
                await previousSchema.Database.ExecuteSqlRawAsync(statement);
            }
        }
    }

    public async Task AssertAsync(IServiceProvider migratedEngine, TestDatabaseKind kind)
    {
        var engine = migratedEngine.GetRequiredService<IHoldemTable>();
        await using var context = await migratedEngine.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync();

        await AssertTournamentTablesAreEmptyAsync(context, kind);

        foreach (var game in _games)
        {
            var migrated = await engine.ApplyAsync(new HoldemActionGetGame(game.Id));
            migrated.IsSuccess.Should().BeTrue(string.Join("; ", migrated.Errors.Select(error => error.Message)));

            // The same game played from scratch on the current schema, dealt the same cards, is the oracle.
            var expected = await PlayOnCurrentSchemaAsync(game);
            migrated.Value.ShouldAsJToken().BeEquivalentToAsJToken(expected);

            // The seeded game is untouched: the same columns, and its hand is still there.
            (await CountAsync(context, $"""SELECT COUNT(*) AS "Value" FROM {Table("Game", kind)} WHERE "Id" = {GuidLiteral(game.Id, kind)} AND "RaiseCap" = 4 AND "Version" = 3""")).Should().Be(1);
            (await CountAsync(context, $"""SELECT COUNT(*) AS "Value" FROM {Table("Hand", kind)} WHERE "GameId" = {GuidLiteral(game.Id, kind)}""")).Should().Be(1);
        }
    }

    private static async Task AssertTournamentTablesAreEmptyAsync(DbContext context, TestDatabaseKind kind)
    {
        foreach (var name in new[] { "Tournament", "TournamentEntry", "TournamentTable" })
        {
            (await CountAsync(context, $"""SELECT COUNT(*) AS "Value" FROM {Table(name, kind)}""")).Should().Be(0, name);
        }
    }

    private static async Task<int> CountAsync(DbContext context, string query)
        => await context.Database.SqlQueryRaw<int>(query).SingleAsync();

    private static async Task<GameData> PlayOnCurrentSchemaAsync(SeededGame game)
    {
        var holes = game.Seats.ToDictionary(seat => seat.SeatNo, seat => new[] { Card.Parse(seat.First), Card.Parse(seat.Second) });
        var shuffler = new TestCardShuffler((_, seatNo) => holes[seatNo], _ => []);
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler);

        var options = new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = game.MaxPlayersCount, InitialSmallBlind = game.SmallBlind, InitialDealerSeatNo = game.DealerSeatNo };
        (await engine.ApplyAsync(new HoldemActionCreateGame(game.Id, options))).IsSuccess.Should().BeTrue();
        var started = await engine.ApplyAsync(new HoldemActionStartGame(game.Id,
            [.. game.Seats.Select(seat => new PlayerJoinData(seat.Name, seat.SeatNo, seat.BuyIn))]));
        started.IsSuccess.Should().BeTrue(string.Join("; ", started.Errors.Select(error => error.Message)));
        var folded = await engine.ApplyAsync(new HoldemActionPlay(game.Id, PlayerActionType.Fold, 0));
        folded.IsSuccess.Should().BeTrue(string.Join("; ", folded.Errors.Select(error => error.Message)));
        return folded.Value;
    }

    /// <summary>The InitialCreate table: in the <c>tce_holdem</c> schema on a server, prefixed on SQLite.</summary>
    private static string Table(string name, TestDatabaseKind kind)
        => kind == TestDatabaseKind.Sqlite ? $"\"{HoldemSchema.SqliteTablePrefix}{name}\"" : $"\"{HoldemSchema.DatabaseSchema}\".\"{name}\"";

    /// <summary>EF stores a SQLite <see cref="Guid"/> as upper-case text and compares it as text.</summary>
    private static string GuidLiteral(Guid id, TestDatabaseKind kind)
        => kind == TestDatabaseKind.Sqlite ? $"'{id.ToString().ToUpperInvariant()}'" : $"'{id}'";

    private static string FalseLiteral(TestDatabaseKind kind) => kind == TestDatabaseKind.PostgreSql ? "FALSE" : "0";

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private sealed record SeededGame(
        Guid Id,
        int MaxPlayersCount,
        decimal SmallBlind,
        byte DealerSeatNo,
        (byte SeatNo, string Name, decimal BuyIn, string First, string Second, decimal FinishStack, decimal Net, uint Strength)[] Seats,
        (byte SeatNo, byte NextSeatNo, string Street, string NextStreet, string ActionType, decimal Size, decimal Amount, decimal AddAmount)[] Actions);
}
