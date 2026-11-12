using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Tests;

/// <summary>
/// <c>StartTournament</c>: the seat draw, the tables opened as games with hand 1 dealt at the first level, the level
/// clock, the guard that keeps every action but <c>Play</c> and <c>GetGame</c> off a tournament table, and the versions.
/// </summary>
[TestClass]
public sealed class TournamentStartTests
{
    private static readonly Guid _tournamentId = Guid.Parse("{72272272-2722-7227-2272-272272272272}");

    private static readonly TimeSpan _levelDuration = TimeSpan.FromMinutes(10);

    private static TournamentOptionData Options(int maxPlayersPerTable = 6, HoldemTableFormat format = HoldemTableFormat.SixMax, int minEntries = 2) => new()
    {
        TableFormat = format,
        MaxPlayersPerTable = maxPlayersPerTable,
        MinEntries = minEntries,
        StartingStack = 5000,
        Levels =
        [
            new TournamentLevelData { SmallBlind = 25, Ante = 5, Duration = _levelDuration },
            new TournamentLevelData { SmallBlind = 50, Ante = 10, Duration = _levelDuration },
            new TournamentLevelData { SmallBlind = 100, Duration = _levelDuration },
        ],
    };

    [TestMethod]
    public async Task StartTournament_NineEntriesSixPerTable_DrawsTwoTablesOfFiveAndFour()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 9);

        var started = await StartAsync(tournament, 42);

        started.State.Should().Be(TournamentState.Running);
        started.Tables.Select(candidate => (candidate.TableNo, candidate.PlayerCount)).Should().Equal((1, 5), (2, 4));
        started.Tables.Select(candidate => candidate.GameId).Should().OnlyHaveUniqueItems();
        started.Entries.Should().HaveCount(9);
        started.Entries.Should().OnlyContain(entry => entry.State == TournamentEntryState.Seated && entry.TableNo != null && entry.SeatNo != null);
        started.Entries.Select(entry => (entry.TableNo, entry.SeatNo)).Should().OnlyHaveUniqueItems("every entry is seated once");
        foreach (var tableData in started.Tables)
        {
            started.Entries.Where(entry => entry.TableNo == tableData.TableNo).Select(entry => (int)entry.SeatNo!.Value)
                .Should().BeEquivalentTo(Enumerable.Range(1, tableData.PlayerCount), "a table is seated from seat 1 up");
        }

        foreach (var tableData in started.Tables)
        {
            var game = await GetGameAsync(table, tableData.GameId);
            game.Options.MaxPlayersCount.Should().Be(6);
            game.Options.InitialSmallBlind.Should().Be(25);
            game.Options.Ante.Should().Be(5);
            game.Options.BlindSchedule.Should().BeNull("the tournament's levels run on its clock, not on a schedule");
            game.Options.AllowTopUp.Should().BeFalse();
            game.Options.AllowRebuy.Should().BeFalse();
            game.Options.MinBuyIn.Should().BeNull();
            game.Options.MaxBuyIn.Should().BeNull();

            var hand = game.HandData!;
            hand.No.Should().Be(1);
            hand.DealerSeatNo.Should().Be(game.Options.InitialDealerSeatNo, "the drawn button deals hand 1");
            hand.SmallBlind.Should().Be(25);
            hand.BigBlind.Should().Be(50);
            hand.Ante.Should().Be(5);
            hand.BlindLevel.Should().Be(0);
            hand.Seats.Should().HaveCount(tableData.PlayerCount);
            hand.Seats.Should().OnlyContain(seat => seat.StartHandStack == 5000);
            hand.Seats.Select(seat => seat.Name).Should().BeEquivalentTo(
                started.Entries.Where(entry => entry.TableNo == tableData.TableNo).Select(entry => entry.Name));
            hand.Actions.Where(action => action.ActionType == HandActionType.Ante).Select(action => action.SeatNo)
                .Should().BeEquivalentTo(hand.Seats.Select(seat => seat.SeatNo), "every seat posts the classic ante of level 0");
            hand.Actions.Should().OnlyContain(action => action.ActionType != HandActionType.Ante || action.Amount == 5);
        }
    }

    [TestMethod]
    public async Task StartTournament_SameSeedTwice_GivesTheSameDraw()
    {
        var first = await DrawAsync(seed: 42);
        var second = await DrawAsync(seed: 42);

        second.Should().Equal(first);
    }

    [TestMethod]
    public async Task StartTournament_WithoutSeed_StoresTheSeedItDrew_WhichReproducesTheDraw()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out var contextFactory);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 9);

        var started = await StartAsync(tournament, null);

        using var dbContext = await contextFactory.CreateDbContextAsync();
        var seed = (await dbContext.Set<Tournament>().SingleAsync(candidate => candidate.Id == _tournamentId)).SeatDrawSeed;
        seed.Should().NotBeNull();

        (await DrawAsync(seed!.Value)).Should().Equal(await SeatsAndButtonsAsync(table, started));
    }

    [TestMethod]
    public async Task StartTournament_TwoEntriesHeadsUp_OneTableWithHeadsUpPositions()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(2, HoldemTableFormat.HeadsUp), 2);

        var started = await StartAsync(tournament, 42);

        started.Tables.Should().ContainSingle().Which.PlayerCount.Should().Be(2);
        var game = await GetGameAsync(table, started.Tables[0].GameId);
        game.Options.TableFormat.Should().Be(HoldemTableFormat.HeadsUp);
        var hand = game.HandData!;
        hand.SmallBlindSeatNo.Should().Be(hand.DealerSeatNo, "heads-up, the button posts the small blind");
        hand.BigBlindSeatNo.Should().NotBe(hand.DealerSeatNo);
        hand.NextPlayerSeatNo.Should().Be(hand.DealerSeatNo, "heads-up, the button acts first pre-flop");
    }

    [TestMethod]
    public async Task StartTournament_TableDrawnWithOnePlayer_GetsItsGameAndSeatButNoHand()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(2, HoldemTableFormat.HeadsUp), 3);

        var started = await StartAsync(tournament, 42);

        started.Tables.Select(candidate => candidate.PlayerCount).Should().Equal(2, 1);
        started.Entries.Single(entry => entry.TableNo == 2).SeatNo.Should().Be(1);
        (await GetGameAsync(table, started.Tables[0].GameId)).HandData.Should().NotBeNull();
        (await GetGameAsync(table, started.Tables[1].GameId)).HandData.Should().BeNull("a lone player waits for a table mate");
    }

    [TestMethod]
    public async Task StartTournament_FewerThanMinEntries_NotEnoughPlayers()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateWithEntriesAsync(tournament, Options(minEntries: 4), 3);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, 42)), HoldemErrorCode.NotEnoughPlayers);

        var data = (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;
        data.State.Should().Be(TournamentState.Registering);
        data.Version.Should().Be(4, "nothing was saved");
        data.Tables.Should().BeEmpty();
    }

    [TestMethod]
    public async Task StartTournament_Twice_TournamentAlreadyStarted()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateWithEntriesAsync(tournament, Options(), 4);
        await StartAsync(tournament, 42);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, 42)), HoldemErrorCode.TournamentAlreadyStarted);
    }

    [TestMethod]
    public async Task Register_AfterStart_TournamentAlreadyStarted()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateWithEntriesAsync(tournament, Options(), 4);
        await StartAsync(tournament, 42);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Late")), HoldemErrorCode.TournamentAlreadyStarted);
        AssertFailed(await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1)), HoldemErrorCode.TournamentAlreadyStarted);
    }

    [TestMethod]
    public async Task StartTournament_UnknownTournament_TournamentNotFound()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId)), HoldemErrorCode.TournamentNotFound);
    }

    [TestMethod]
    public async Task LevelClock_FollowsTheTimeSinceTheStart()
    {
        var clock = new TestTimeProvider();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, timeProvider: clock);
        await CreateWithEntriesAsync(tournament, Options(), 4);
        var startedAt = clock.UtcNow;

        var started = await StartAsync(tournament, 42);

        started.StartedAt.Should().Be(startedAt);
        started.LevelIndex.Should().Be(0);
        started.LevelEndsAt.Should().Be(startedAt + _levelDuration);

        clock.UtcNow = startedAt + TimeSpan.FromMinutes(9) + TimeSpan.FromSeconds(59);
        var beforeTheEnd = await GetAsync(tournament);
        beforeTheEnd.LevelIndex.Should().Be(0);
        beforeTheEnd.LevelEndsAt.Should().Be(startedAt + _levelDuration);

        clock.UtcNow = startedAt + _levelDuration;
        var atTheEnd = await GetAsync(tournament);
        atTheEnd.LevelIndex.Should().Be(1);
        atTheEnd.LevelEndsAt.Should().Be(startedAt + (2 * _levelDuration));

        clock.UtcNow = startedAt + (2 * _levelDuration);
        var lastLevel = await GetAsync(tournament);
        lastLevel.LevelIndex.Should().Be(2);
        lastLevel.LevelEndsAt.Should().BeNull("the last level lasts until the tournament ends");

        clock.UtcNow = startedAt + TimeSpan.FromDays(30);
        var farPast = await GetAsync(tournament);
        farPast.LevelIndex.Should().Be(2, "far past the last level the clock stays on it");
        farPast.LevelEndsAt.Should().BeNull();
        farPast.Version.Should().Be(started.Version, "reading the clock saves nothing");
    }

    [TestMethod]
    public async Task LevelClock_BeforeTheStart_IsLevelZeroWithNoEnd()
    {
        var clock = new TestTimeProvider();
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, timeProvider: clock);
        await CreateWithEntriesAsync(tournament, Options(), 2);

        clock.Advance(TimeSpan.FromHours(5));
        var data = await GetAsync(tournament);

        data.LevelIndex.Should().Be(0);
        data.LevelEndsAt.Should().BeNull();
        data.StartedAt.Should().BeNull();
    }

    [TestMethod]
    public async Task DealLevel_OverridesTheGamesOwnBlinds()
    {
        using var host = TestEngineHost.Setup(out var table, out var contextFactory);
        var gameId = Guid.NewGuid();
        (await table.ApplyAsync(HoldemAction.CreateGame(new GameOptionData { InitialSmallBlind = 25, Ante = 5 }, gameId))).IsSuccess.Should().BeTrue();

        using (var dbContext = await contextFactory.CreateDbContextAsync())
        {
            var game = await dbContext.LoadGameWithCurrentHandAsync(gameId);
            var error = await new StartGameActionHandler(null, new DealLevel(3, 100, null))
                .ImplementHandleAsync(HoldemAction.StartGame(gameId, [new PlayerJoinData("A", 1, 5000), new PlayerJoinData("B", 2, 5000)]), game, default);
            error.Should().BeNull();
            await dbContext.SaveChangesAsync();
        }

        var hand = (await GetGameAsync(table, gameId)).HandData!;
        hand.SmallBlind.Should().Be(100);
        hand.BigBlind.Should().Be(200);
        hand.BlindLevel.Should().Be(3);
        hand.Ante.Should().Be(0, "the level has no ante, whatever the game's own");
        hand.Actions.Should().NotContain(action => action.ActionType == HandActionType.Ante);
    }

    [TestMethod]
    [DataRow("StartHand")]
    [DataRow("LeaveSeat")]
    [DataRow("TopUp")]
    [DataRow("KickSeat")]
    [DataRow("FinishGame")]
    [DataRow("StartGame")]
    [DataRow("CreateGame")]
    public async Task TournamentTable_ManagedActions_ManagedByTournament(string actionName)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 4);
        var gameId = (await StartAsync(tournament, 42)).Tables[0].GameId;
        var before = await GetGameAsync(table, gameId);

        var result = await table.ApplyAsync(ManagedAction(actionName, gameId));

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.ManagedByTournament);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await GetGameAsync(table, gameId)).ShouldAsJToken().BeEquivalentToAsJToken(before);
    }

    [TestMethod]
    public async Task TournamentTable_PlayAndGetGame_Work()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 4);
        var gameId = (await StartAsync(tournament, 42)).Tables[0].GameId;

        var before = await GetGameAsync(table, gameId);
        var played = await table.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold));

        played.IsSuccess.Should().BeTrue(string.Join(", ", played.Errors));
        played.Value.Version.Should().Be(before.Version + 1);
        played.Value.HandData!.Actions.Should().HaveCount(before.HandData!.Actions.Count + 1);
    }

    [TestMethod]
    [DataRow("StartHand")]
    [DataRow("LeaveSeat")]
    [DataRow("TopUp")]
    [DataRow("KickSeat")]
    [DataRow("FinishGame")]
    public async Task CashGame_NextToATournament_IsUnaffected(string actionName)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 4);
        await StartAsync(tournament, 42);

        var gameId = Guid.NewGuid();
        (await table.ApplyAsync(HoldemAction.CreateGame(new GameOptionData { AllowTopUp = true }, gameId))).IsSuccess.Should().BeTrue();
        (await table.ApplyAsync(HoldemAction.StartGame(gameId, [new PlayerJoinData("A", 1, 100), new PlayerJoinData("B", 2, 100)]))).IsSuccess.Should().BeTrue();
        (await table.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold))).IsSuccess.Should().BeTrue();

        var result = await table.ApplyAsync(ManagedAction(actionName, gameId));

        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
    }

    [TestMethod]
    public async Task StartTournament_RaisesTheVersionByOne_AndTheGamesHaveTheirHandlersVersions()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        var registered = await CreateWithEntriesAsync(tournament, Options(2, HoldemTableFormat.HeadsUp), 3);

        var started = await StartAsync(tournament, 42);

        started.Version.Should().Be(registered.Version + 1);
        foreach (var tableData in started.Tables)
        {
            (await GetGameAsync(table, tableData.GameId)).Version.Should().Be(1, "the games are created in the tournament's save, as CreateGame creates them");
        }
    }

    [TestMethod]
    public async Task StartTournament_StaleExpectedVersion_Conflict()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var registered = await CreateWithEntriesAsync(tournament, Options(), 2);

        var result = await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, 42) with { ExpectedVersion = registered.Version - 1 });

        AssertFailed(result, HoldemErrorCode.Conflict);
        (await GetAsync(tournament)).State.Should().Be(TournamentState.Registering);
    }

    [TestMethod]
    public async Task StartTournament_PrizePool_IsBuyInTimesEntries()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateWithEntriesAsync(tournament, Options() with { BuyIn = 20 }, 5);

        (await StartAsync(tournament, 42)).PrizePool.Should().Be(100);
    }

    private static HoldemAction ManagedAction(string actionName, Guid gameId) => actionName switch
    {
        "StartHand" => HoldemAction.StartHand(gameId),
        "LeaveSeat" => HoldemAction.LeaveSeat(gameId, 1),
        "TopUp" => HoldemAction.TopUp(gameId, 1, 10),
        "KickSeat" => HoldemAction.KickSeat(gameId, 1),
        "FinishGame" => HoldemAction.FinishGame(gameId),
        "StartGame" => HoldemAction.StartGame(gameId, [new PlayerJoinData("X", 1, 100), new PlayerJoinData("Y", 2, 100)]),
        "CreateGame" => HoldemAction.CreateGame(new GameOptionData(), gameId),
        _ => throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null),
    };

    /// <summary>Starts a fresh tournament of nine entries and returns where each entry sits and each table's button.</summary>
    private static async Task<List<string>> DrawAsync(int seed)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        var table = host.GetRequiredService<IHoldemTable>();
        await CreateWithEntriesAsync(tournament, Options(), 9);
        return await SeatsAndButtonsAsync(table, await StartAsync(tournament, seed));
    }

    private static async Task<List<string>> SeatsAndButtonsAsync(IHoldemTable table, TournamentData started)
    {
        var draw = started.Entries.Select(entry => $"{entry.Name}@{entry.TableNo}/{entry.SeatNo}").ToList();
        foreach (var tableData in started.Tables)
        {
            draw.Add($"button@{tableData.TableNo}/{(await GetGameAsync(table, tableData.GameId)).HandData!.DealerSeatNo}");
        }

        return draw;
    }

    private static async Task<TournamentData> CreateWithEntriesAsync(IHoldemTournament tournament, TournamentOptionData options, int entryCount)
    {
        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(options, _tournamentId));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        for (var entry = 1; entry <= entryCount; entry++)
        {
            result = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, $"Player {entry}"));
            result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        }

        return result.Value;
    }

    private static async Task<TournamentData> StartAsync(IHoldemTournament tournament, int? seed)
    {
        var result = await tournament.ApplyAsync(TournamentAction.StartTournament(_tournamentId, seed));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static async Task<TournamentData> GetAsync(IHoldemTournament tournament)
        => (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;

    private static async Task<GameData> GetGameAsync(IHoldemTable table, Guid gameId)
    {
        var result = await table.ApplyAsync(HoldemAction.GetGame(gameId));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static void AssertFailed(HoldemResult<TournamentData> result, HoldemErrorCode code)
    {
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
    }
}
