using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests.Persistence.Upgrades;

/// <summary>
/// <c>AddGameTableFormat</c> adds <c>Game.TableFormat</c>. A heads-up and a six-seat game, each in the
/// middle of its first hand, must come through it intact and stored as six-max, the only format that
/// existed before the column.
/// </summary>
internal sealed class AddGameTableFormatUpgradeFixture : IUpgradeFixture
{
    // The rows StartGame writes for these games at InitialCreate: the blinds are posted and the first
    // seat to act is on the clock. Seats are (seat, name, buy-in, hole cards); blinds are
    // (seat, next seat, action type, size, amount, add amount), as the ledger stores them.
    private static readonly SeededGame[] _games =
    [
        new(Guid.Parse("2a0e5c1b-7d4f-4c3a-9b8e-1f2d3c4b5a6e"), MaxPlayersCount: 2, SmallBlind: 50M, DealerSeatNo: 1,
            Seats: [(1, "Alice", 10_000M, "kd", "8s"), (2, "Bob", 10_000M, "jd", "ks")],
            Blinds: [(1, 2, "SmallBlind", 50M, 50M, 50M), (2, 1, "BigBlind", 100M, 100M, 50M)]),
        new(Guid.Parse("6b1f7e2d-3c4a-4e5f-8a9b-0c1d2e3f4a5b"), MaxPlayersCount: 6, SmallBlind: 1M, DealerSeatNo: 3,
            Seats:
            [
                (1, "P1", 200M, "td", "ac"), (2, "P2", 200M, "ks", "jc"), (3, "P3", 200M, "jh", "3d"),
                (4, "P4", 200M, "8h", "3s"), (5, "P5", 200M, "9s", "4c"), (6, "P6", 200M, "4h", "9h"),
            ],
            Blinds: [(4, 5, "SmallBlind", 1M, 1M, 1M), (5, 6, "BigBlind", 2M, 2M, 1M)]),
    ];

    public string MigrationName => "AddGameTableFormat";

    public async Task SeedAsync(DbContext previousSchema, TestDatabaseKind kind)
    {
        foreach (var game in _games)
        {
            var id = GuidLiteral(game.Id, kind);
            var statements = new List<string>
            {
                $"""INSERT INTO {Table("Game", kind)} ("Id", "InitialSmallBlind", "MaxPlayersCount", "InitialDealerSeatNo", "GameType", "BettingStructure", "IsFinished", "Version") VALUES ({id}, {Number(game.SmallBlind)}, {game.MaxPlayersCount}, {game.DealerSeatNo}, 'Holdem', 'NoLimit', {FalseLiteral(kind)}, 2)""",
                $"""INSERT INTO {Table("Hand", kind)} ("GameId", "No", "DealerSeatNo") VALUES ({id}, 1, {game.DealerSeatNo})""",
            };

            foreach (var (seatNo, name, buyIn, first, second) in game.Seats)
            {
                statements.Add($"""INSERT INTO {Table("Player", kind)} ("GameId", "SeatNo", "HandNoStart", "Name", "HandNoEnd") VALUES ({id}, {seatNo}, 1, '{name}', NULL)""");
                statements.Add($"""INSERT INTO {Table("BuyIn", kind)} ("GameId", "SeatNo", "HandNo", "Amount") VALUES ({id}, {seatNo}, 1, {Number(buyIn)})""");
                statements.Add($"""INSERT INTO {Table("HandPlayerStartState", kind)} ("GameId", "HandNo", "SeatNo", "StartStack") VALUES ({id}, 1, {seatNo}, {Number(buyIn)})""");
                statements.Add($"""INSERT INTO {Table("HandHoleCard", kind)} ("GameId", "HandNo", "SeatNo", "Sequence", "Card") VALUES ({id}, 1, {seatNo}, 1, '{first}'), ({id}, 1, {seatNo}, 2, '{second}')""");
            }

            var actionNo = 0;
            foreach (var (seatNo, nextSeatNo, actionType, size, amount, addAmount) in game.Blinds)
            {
                statements.Add($"""INSERT INTO {Table("HandAction", kind)} ("GameId", "HandNo", "No", "SeatNo", "NextSeatNo", "Street", "NextStreet", "ActionType", "Size", "Amount", "AddAmount", "Paid") VALUES ({id}, 1, {++actionNo}, {seatNo}, {nextSeatNo}, 'PreFlop', 'PreFlop', '{actionType}', {Number(size)}, {Number(amount)}, {Number(addAmount)}, 0)""");
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

        foreach (var game in _games)
        {
            var migrated = await engine.ApplyAsync(new HoldemActionGetGame(game.Id));
            migrated.IsSuccess.Should().BeTrue(string.Join("; ", migrated.Errors.Select(error => error.Message)));

            // The same game played from scratch on the current schema, dealt the same cards, is the oracle.
            var expected = await PlayOnCurrentSchemaAsync(game);
            migrated.Value.ShouldAsJToken().BeEquivalentToAsJToken(expected);

            // GameData does not expose the format yet, so the stored column is read directly.
            var query = $"""SELECT "TableFormat" AS "Value" FROM {Table("Game", kind)} WHERE "Id" = {GuidLiteral(game.Id, kind)}""";
            var tableFormat = await context.Database.SqlQueryRaw<string>(query).SingleAsync();
            tableFormat.Should().Be(nameof(HoldemTableFormat.SixMax), "games stored before the column existed are six-max");
        }
    }

    private static async Task<GameData> PlayOnCurrentSchemaAsync(SeededGame game)
    {
        var holes = game.Seats.ToDictionary(seat => seat.SeatNo, seat => new[] { Card.Parse(seat.First), Card.Parse(seat.Second) });
        var shuffler = new TestCardShuffler((_, seatNo) => holes[seatNo], _ => []);
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler);

        var options = new GameOptionData { MaxPlayersCount = game.MaxPlayersCount, InitialSmallBlind = game.SmallBlind, InitialDealerSeatNo = game.DealerSeatNo };
        (await engine.ApplyAsync(new HoldemActionCreateGame(game.Id, options))).IsSuccess.Should().BeTrue();
        var started = await engine.ApplyAsync(new HoldemActionStartGame(game.Id,
            [.. game.Seats.Select(seat => new PlayerJoinData(seat.Name, seat.SeatNo, seat.BuyIn))]));
        started.IsSuccess.Should().BeTrue(string.Join("; ", started.Errors.Select(error => error.Message)));
        return started.Value;
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
        (byte SeatNo, string Name, decimal BuyIn, string First, string Second)[] Seats,
        (byte SeatNo, byte NextSeatNo, string ActionType, decimal Size, decimal Amount, decimal AddAmount)[] Blinds);
}
