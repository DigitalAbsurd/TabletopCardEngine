using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.Tests.Persistence;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The registration phase of a tournament through <see cref="IHoldemTournament"/>: create, read, register and
/// unregister, every validation rule, the version sequence and the value round-trip of the options.
/// </summary>
[TestClass]
public sealed class TournamentRegistrationTests
{
    private static readonly Guid _tournamentId = Guid.Parse("{72172172-1721-7217-2172-172172172172}");

    private static readonly TournamentLevelData _level = new() { SmallBlind = 25, Duration = TimeSpan.FromMinutes(10) };

    private static TournamentOptionData ValidOptions() => new() { Levels = [_level] };

    [TestMethod]
    public async Task CreateTournament_ValidOptions_IsRegisteringAtVersionOne()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(ValidOptions(), _tournamentId));

        result.IsSuccess.Should().BeTrue();
        var data = result.Value;
        data.TournamentId.Should().Be(_tournamentId);
        data.State.Should().Be(TournamentState.Registering);
        data.Version.Should().Be(1);
        data.Entries.Should().BeEmpty();
        data.Tables.Should().BeEmpty();
        data.LevelIndex.Should().Be(0);
        data.LevelEndsAt.Should().BeNull();
        data.StartedAt.Should().BeNull();
        data.FinishedAt.Should().BeNull();
        data.PrizePool.Should().Be(0);
    }

    [TestMethod]
    public async Task CreateTournament_WithoutId_GeneratesOne()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(ValidOptions()));

        result.IsSuccess.Should().BeTrue();
        result.Value.TournamentId.Should().NotBe(Guid.Empty);
    }

    [TestMethod]
    public void TournamentOptionData_Defaults_MatchTheContract()
    {
        var options = new TournamentOptionData();

        options.BettingStructure.Should().Be(BettingStructure.NoLimit);
        options.TableFormat.Should().Be(HoldemTableFormat.SixMax);
        options.MaxPlayersPerTable.Should().Be(6);
        options.RaiseCap.Should().Be(4);
        options.AnteType.Should().Be(AnteType.Classic);
        options.StartingStack.Should().Be(10000);
        options.BuyIn.Should().Be(0);
        options.MinEntries.Should().Be(2);
        options.MaxEntries.Should().BeNull();
        options.Levels.Should().BeEmpty();
        options.Payouts.Should().Equal(100m);
        options.Should().Be(new TournamentOptionData(), "the defaults compare by value");
    }

    [TestMethod]
    public async Task CreateTournament_Options_RoundTripByValue()
    {
        var options = new TournamentOptionData
        {
            BettingStructure = BettingStructure.FixedLimit,
            TableFormat = HoldemTableFormat.FullRing,
            MaxPlayersPerTable = 9,
            RaiseCap = 5,
            AnteType = AnteType.BigBlind,
            StartingStack = 1500.50m,
            BuyIn = 12.25m,
            MinEntries = 3,
            MaxEntries = 90,
            Levels =
            [
                new TournamentLevelData { SmallBlind = 10, Duration = TimeSpan.FromMinutes(1) },
                new TournamentLevelData { SmallBlind = 10, Ante = 2.5m, Duration = TimeSpan.FromSeconds(61) },
                new TournamentLevelData { SmallBlind = 15.75m, Ante = 3, Duration = TimeSpan.FromHours(2) },
            ],
            Payouts = [50, 30.5m, 19.5m],
        };

        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var created = await tournament.ApplyAsync(TournamentAction.CreateTournament(options, _tournamentId));
        var read = await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId));

        created.IsSuccess.Should().BeTrue();
        read.IsSuccess.Should().BeTrue();
        read.Value.Options.ShouldAsJToken().BeEquivalentToAsJToken(options);
        read.Value.Options.Should().Be(created.Value.Options, "options rebuilt from the database compare by value");
        read.Value.Should().Be(created.Value, "reading changes nothing");
        read.Value.Options.GetHashCode().Should().Be(created.Value.Options.GetHashCode());
    }

    [TestMethod]
    public async Task CreateTournament_ExistingId_TournamentAlreadyExists()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        (await tournament.ApplyAsync(TournamentAction.CreateTournament(ValidOptions(), _tournamentId))).IsSuccess.Should().BeTrue();

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(ValidOptions(), _tournamentId));

        AssertFailed(result, HoldemErrorCode.TournamentAlreadyExists);
    }

    [TestMethod]
    public async Task CreateTournament_UndefinedBettingStructure_UnsupportedGameOptions()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(
            ValidOptions() with { BettingStructure = (BettingStructure)99 }, _tournamentId));

        AssertFailed(result, HoldemErrorCode.UnsupportedGameOptions);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Errors.Single().Code
            .Should().Be(HoldemErrorCode.TournamentNotFound, "a rejected create saves nothing");
    }

    private static readonly Dictionary<string, TournamentOptionData> _invalidOptions = new()
    {
        ["TableFormatUndefined"] = ValidOptions() with { TableFormat = (HoldemTableFormat)99 },
        ["HeadsUpWithThreeSeats"] = ValidOptions() with { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersPerTable = 3 },
        ["SixMaxWithOneSeat"] = ValidOptions() with { MaxPlayersPerTable = 1 },
        ["SixMaxWithSevenSeats"] = ValidOptions() with { MaxPlayersPerTable = 7 },
        ["FullRingWithSixSeats"] = ValidOptions() with { TableFormat = HoldemTableFormat.FullRing, MaxPlayersPerTable = 6 },
        ["FullRingWithElevenSeats"] = ValidOptions() with { TableFormat = HoldemTableFormat.FullRing, MaxPlayersPerTable = 11 },
        ["RaiseCapZero"] = ValidOptions() with { RaiseCap = 0 },
        ["AnteTypeUndefined"] = ValidOptions() with { AnteType = (AnteType)99 },
        ["StartingStackZero"] = ValidOptions() with { StartingStack = 0 },
        ["StartingStackNegative"] = ValidOptions() with { StartingStack = -1 },
        ["StartingStackThreeDecimals"] = ValidOptions() with { StartingStack = 100.001m },
        ["BuyInNegative"] = ValidOptions() with { BuyIn = -0.01m },
        ["BuyInThreeDecimals"] = ValidOptions() with { BuyIn = 10.005m },
        ["MinEntriesOne"] = ValidOptions() with { MinEntries = 1 },
        ["MaxEntriesBelowMinEntries"] = ValidOptions() with { MinEntries = 3, MaxEntries = 2 },
        ["LevelsNull"] = ValidOptions() with { Levels = null! },
        ["LevelsEmpty"] = ValidOptions() with { Levels = [] },
        ["LevelsMoreThanOneHundred"] = ValidOptions() with { Levels = [.. Enumerable.Repeat(_level, 101)] },
        ["LevelNull"] = ValidOptions() with { Levels = [null!] },
        ["SmallBlindZero"] = ValidOptions() with { Levels = [_level with { SmallBlind = 0 }] },
        ["SmallBlindNegative"] = ValidOptions() with { Levels = [_level with { SmallBlind = -25 }] },
        ["SmallBlindThreeDecimals"] = ValidOptions() with { Levels = [_level with { SmallBlind = 0.125m }] },
        ["AnteNegative"] = ValidOptions() with { Levels = [_level with { Ante = -1 }] },
        ["AnteThreeDecimals"] = ValidOptions() with { Levels = [_level with { Ante = 0.125m }] },
        ["DurationUnderAMinute"] = ValidOptions() with { Levels = [_level with { Duration = TimeSpan.FromSeconds(59) }] },
        ["DurationNotWholeSeconds"] = ValidOptions() with { Levels = [_level with { Duration = TimeSpan.FromMilliseconds(60_500) }] },
        ["DurationNegative"] = ValidOptions() with { Levels = [_level with { Duration = TimeSpan.FromMinutes(-10) }] },
        ["SmallBlindsDecreasing"] = ValidOptions() with { Levels = [_level with { SmallBlind = 50 }, _level] },
        ["LevelsJsonOverTheColumn"] = ValidOptions() with
        {
            Levels = [.. Enumerable.Repeat(new TournamentLevelData { SmallBlind = 123456789012.34m, Ante = 123456789012.34m, Duration = TimeSpan.FromDays(365) }, 100)],
        },
        ["PayoutsNull"] = ValidOptions() with { Payouts = null! },
        ["PayoutsEmpty"] = ValidOptions() with { Payouts = [] },
        ["PayoutsMoreThanMinEntries"] = ValidOptions() with { Payouts = [50, 30, 20] },
        ["PayoutZero"] = ValidOptions() with { Payouts = [100, 0] },
        ["PayoutNegative"] = ValidOptions() with { Payouts = [110, -10] },
        ["PayoutsIncreasing"] = ValidOptions() with { Payouts = [40, 60] },
        ["PayoutsBelowOneHundred"] = ValidOptions() with { Payouts = [60, 39.99m] },
        ["PayoutsAboveOneHundred"] = ValidOptions() with { Payouts = [60, 40.01m] },
        ["PayoutThreeDecimals"] = ValidOptions() with { Payouts = [50.005m, 49.995m] },
    };

    [TestMethod]
    [DataRow("TableFormatUndefined")]
    [DataRow("HeadsUpWithThreeSeats")]
    [DataRow("SixMaxWithOneSeat")]
    [DataRow("SixMaxWithSevenSeats")]
    [DataRow("FullRingWithSixSeats")]
    [DataRow("FullRingWithElevenSeats")]
    [DataRow("RaiseCapZero")]
    [DataRow("AnteTypeUndefined")]
    [DataRow("StartingStackZero")]
    [DataRow("StartingStackNegative")]
    [DataRow("StartingStackThreeDecimals")]
    [DataRow("BuyInNegative")]
    [DataRow("BuyInThreeDecimals")]
    [DataRow("MinEntriesOne")]
    [DataRow("MaxEntriesBelowMinEntries")]
    [DataRow("LevelsNull")]
    [DataRow("LevelsEmpty")]
    [DataRow("LevelsMoreThanOneHundred")]
    [DataRow("LevelNull")]
    [DataRow("SmallBlindZero")]
    [DataRow("SmallBlindNegative")]
    [DataRow("SmallBlindThreeDecimals")]
    [DataRow("AnteNegative")]
    [DataRow("AnteThreeDecimals")]
    [DataRow("DurationUnderAMinute")]
    [DataRow("DurationNotWholeSeconds")]
    [DataRow("DurationNegative")]
    [DataRow("SmallBlindsDecreasing")]
    [DataRow("LevelsJsonOverTheColumn")]
    [DataRow("PayoutsNull")]
    [DataRow("PayoutsEmpty")]
    [DataRow("PayoutsMoreThanMinEntries")]
    [DataRow("PayoutZero")]
    [DataRow("PayoutNegative")]
    [DataRow("PayoutsIncreasing")]
    [DataRow("PayoutsBelowOneHundred")]
    [DataRow("PayoutsAboveOneHundred")]
    [DataRow("PayoutThreeDecimals")]
    public async Task CreateTournament_InvalidOption_InvalidTournamentOptions(string invalidCase)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(_invalidOptions[invalidCase], _tournamentId));

        AssertFailed(result, HoldemErrorCode.InvalidTournamentOptions);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Errors.Single().Code
            .Should().Be(HoldemErrorCode.TournamentNotFound, "a rejected create saves nothing");
    }

    [TestMethod]
    public async Task CreateTournament_NullOptions_InvalidTournamentOptions()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(null!, _tournamentId));

        AssertFailed(result, HoldemErrorCode.InvalidTournamentOptions);
    }

    private static readonly Dictionary<string, TournamentOptionData> _edgeOptions = new()
    {
        ["HeadsUp"] = ValidOptions() with { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersPerTable = 2 },
        ["SixMaxWithTwoSeats"] = ValidOptions() with { MaxPlayersPerTable = 2 },
        ["FullRingWithSevenSeats"] = ValidOptions() with { TableFormat = HoldemTableFormat.FullRing, MaxPlayersPerTable = 7 },
        ["FullRingWithTenSeats"] = ValidOptions() with { TableFormat = HoldemTableFormat.FullRing, MaxPlayersPerTable = 10 },
        ["PotLimit"] = ValidOptions() with { BettingStructure = BettingStructure.PotLimit },
        ["NoRaiseCap"] = ValidOptions() with { RaiseCap = null },
        ["RaiseCapOne"] = ValidOptions() with { RaiseCap = 1 },
        ["StartingStackOneCent"] = ValidOptions() with { StartingStack = 0.01m },
        ["MaxEntriesEqualToMinEntries"] = ValidOptions() with { MinEntries = 5, MaxEntries = 5 },
        // 100 levels are allowed, but at about 49 characters of JSON apiece these fill the column a little past 80.
        ["EightyLevels"] = ValidOptions() with { Levels = [.. Enumerable.Repeat(_level, 80)] },
        ["EqualSmallBlinds"] = ValidOptions() with { Levels = [_level, _level with { Ante = 5 }] },
        ["DurationOfOneMinute"] = ValidOptions() with { Levels = [_level with { Duration = TimeSpan.FromMinutes(1) }] },
        ["EqualPayouts"] = ValidOptions() with { Payouts = [50, 50] },
        ["PayoutsAsManyAsMinEntries"] = ValidOptions() with { MinEntries = 3, Payouts = [33.34m, 33.33m, 33.33m] },
    };

    [TestMethod]
    [DataRow("HeadsUp")]
    [DataRow("SixMaxWithTwoSeats")]
    [DataRow("FullRingWithSevenSeats")]
    [DataRow("FullRingWithTenSeats")]
    [DataRow("PotLimit")]
    [DataRow("NoRaiseCap")]
    [DataRow("RaiseCapOne")]
    [DataRow("StartingStackOneCent")]
    [DataRow("MaxEntriesEqualToMinEntries")]
    [DataRow("EightyLevels")]
    [DataRow("EqualSmallBlinds")]
    [DataRow("DurationOfOneMinute")]
    [DataRow("EqualPayouts")]
    [DataRow("PayoutsAsManyAsMinEntries")]
    public async Task CreateTournament_OptionOnTheEdge_IsAccepted(string edgeCase)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(_edgeOptions[edgeCase], _tournamentId));

        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        result.Value.Options.Should().Be(_edgeOptions[edgeCase] with
        {
            Levels = result.Value.Options.Levels,
            Payouts = result.Value.Options.Payouts,
        });
        result.Value.Options.Levels.Should().Equal(_edgeOptions[edgeCase].Levels);
        result.Value.Options.Payouts.Should().Equal(_edgeOptions[edgeCase].Payouts);
    }

    [TestMethod]
    public async Task UnknownTournament_EveryActionButCreate_TournamentNotFound()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId)), HoldemErrorCode.TournamentNotFound);
        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Alice")), HoldemErrorCode.TournamentNotFound);
        AssertFailed(await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1)), HoldemErrorCode.TournamentNotFound);
        AssertFailed(
            await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Alice") with { ExpectedVersion = 7 }),
            HoldemErrorCode.TournamentNotFound);
    }

    [TestMethod]
    public async Task Register_GivesAscendingEntryNumbers()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());

        await RegisterAsync(tournament, "Alice");
        await RegisterAsync(tournament, "Bob");
        var data = await RegisterAsync(tournament, "Carol");

        data.Entries.Should().Equal(
            new TournamentEntryData(1, "Alice"),
            new TournamentEntryData(2, "Bob"),
            new TournamentEntryData(3, "Carol"));
        data.Entries.Should().OnlyContain(entry => entry.State == TournamentEntryState.Registered
            && entry.TableNo == null && entry.SeatNo == null && entry.Place == null && entry.Prize == 0);
    }

    [TestMethod]
    public async Task Register_AfterUnregister_NeverReusesAnEntryNumber()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        await RegisterAsync(tournament, "Bob");

        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 2))).IsSuccess.Should().BeTrue();
        var data = await RegisterAsync(tournament, "Carol");

        data.Entries.Select(entry => (entry.EntryNo, entry.Name)).Should().Equal((1, "Alice"), (3, "Carol"));

        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 3))).IsSuccess.Should().BeTrue();
        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1))).IsSuccess.Should().BeTrue();
        data = await RegisterAsync(tournament, "Dave");

        data.Entries.Should().Equal(new TournamentEntryData(4, "Dave"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\n")]
    public async Task Register_BlankName_InvalidEntry(string? name)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());

        var result = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, name!));

        AssertFailed(result, HoldemErrorCode.InvalidEntry);
        await AssertVersionAsync(tournament, 1);
    }

    [TestMethod]
    public async Task Register_NameLength_IsCappedAtSixtyFour()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, new string('x', 65))), HoldemErrorCode.InvalidEntry);
        var data = await RegisterAsync(tournament, new string('x', 64));

        data.Entries.Single().Name.Should().HaveLength(64);
    }

    [TestMethod]
    public async Task Register_NameAlreadyRegistered_IgnoringCase_InvalidEntry()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Alice")), HoldemErrorCode.InvalidEntry);
        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "ALICE")), HoldemErrorCode.InvalidEntry);
        await AssertVersionAsync(tournament, 2);
    }

    [TestMethod]
    public async Task Register_NameOfAWithdrawnEntry_IsFreeAgain()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1))).IsSuccess.Should().BeTrue();

        var data = await RegisterAsync(tournament, "alice");

        data.Entries.Should().Equal(new TournamentEntryData(2, "alice"));
    }

    [TestMethod]
    public async Task Register_MaxEntriesReached_TournamentFull()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions() with { MaxEntries = 2 });
        await RegisterAsync(tournament, "Alice");
        await RegisterAsync(tournament, "Bob");

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Carol")), HoldemErrorCode.TournamentFull);

        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 2))).IsSuccess.Should().BeTrue();
        var data = await RegisterAsync(tournament, "Carol");
        data.Entries.Select(entry => entry.Name).Should().Equal("Alice", "Carol");
    }

    [TestMethod]
    [DataRow(TournamentState.Running)]
    [DataRow(TournamentState.Finished)]
    public async Task RegisterAndUnregister_NotRegistering_TournamentAlreadyStarted(TournamentState state)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out var contextFactory);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        await SetStateAsync(contextFactory, state);

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Bob")), HoldemErrorCode.TournamentAlreadyStarted);
        AssertFailed(await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1)), HoldemErrorCode.TournamentAlreadyStarted);

        var data = (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;
        data.State.Should().Be(state);
        data.Entries.Should().ContainSingle();
        data.Version.Should().Be(2);
    }

    [TestMethod]
    public async Task Unregister_RemovesTheEntry()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        await RegisterAsync(tournament, "Bob");

        var result = await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().Equal(new TournamentEntryData(2, "Bob"));
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.Should().Be(result.Value);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(-1)]
    public async Task Unregister_UnknownEntryNo_InvalidEntry(int entryNo)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        await RegisterAsync(tournament, "Bob");

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, entryNo)), HoldemErrorCode.InvalidEntry);
        await AssertVersionAsync(tournament, 3);
    }

    [TestMethod]
    public async Task Unregister_Twice_InvalidEntry()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");
        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1))).IsSuccess.Should().BeTrue();

        AssertFailed(await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1)), HoldemErrorCode.InvalidEntry);
    }

    [TestMethod]
    public async Task PrizePool_IsTheBuyInTimesTheEntries()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        (await CreateAsync(tournament, ValidOptions() with { BuyIn = 12.5m })).PrizePool.Should().Be(0);

        (await RegisterAsync(tournament, "Alice")).PrizePool.Should().Be(12.5m);
        (await RegisterAsync(tournament, "Bob")).PrizePool.Should().Be(25m);
        (await RegisterAsync(tournament, "Carol")).PrizePool.Should().Be(37.5m);
        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 2))).Value.PrizePool.Should().Be(25m);
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    public async Task Version_StartsAtOne_AndRisesWithEverySave(TestDatabaseKind database)
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _, database);

        (await CreateAsync(tournament, ValidOptions())).Version.Should().Be(1);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.Version.Should().Be(1);
        (await RegisterAsync(tournament, "Alice")).Version.Should().Be(2);
        (await RegisterAsync(tournament, "Bob")).Version.Should().Be(3);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.Version.Should().Be(3);
        (await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1))).Value.Version.Should().Be(4);
        (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.Version.Should().Be(4);
    }

    [TestMethod]
    public async Task ExpectedVersion_Stale_ReturnsConflict_AndSavesNothing()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());
        await RegisterAsync(tournament, "Alice");

        AssertFailed(
            await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Bob") with { ExpectedVersion = 1 }),
            HoldemErrorCode.Conflict);
        AssertFailed(
            await tournament.ApplyAsync(TournamentAction.Unregister(_tournamentId, 1) with { ExpectedVersion = 3 }),
            HoldemErrorCode.Conflict);
        AssertFailed(
            await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId) with { ExpectedVersion = 1 }),
            HoldemErrorCode.Conflict);

        var data = (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value;
        data.Version.Should().Be(2);
        data.Entries.Should().Equal(new TournamentEntryData(1, "Alice"));
    }

    [TestMethod]
    public async Task ExpectedVersion_Current_IsApplied()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());

        var result = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, "Alice") with { ExpectedVersion = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(2);
    }

    [TestMethod]
    public async Task ExpectedVersion_IsIgnoredByCreate()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);

        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(ValidOptions(), _tournamentId) with { ExpectedVersion = 42 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(1);
    }

    [TestMethod]
    public async Task UnknownActionType_InvalidAction()
    {
        using var host = TestEngineHost.SetupTournament(out var tournament, out _);
        await CreateAsync(tournament, ValidOptions());

        AssertFailed(await tournament.ApplyAsync(new UnknownTournamentAction(_tournamentId)), HoldemErrorCode.InvalidAction);
        await AssertVersionAsync(tournament, 1);
    }

    [TestMethod]
    public void WithTimeProvider_Null_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddTabletopCardEngine(tce => tce.AddHoldem(
            db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")),
            holdem => holdem.WithTimeProvider(null!)));

        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [TestMethod]
    public void WithTimeProvider_IsStoredInTheContainersOptions_AndDefaultsToTheSystemClock()
    {
        var clock = new FixedTimeProvider();
        var withClock = new ServiceCollection();
        withClock.AddTabletopCardEngine(tce => tce.AddHoldem(
            db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")),
            holdem => holdem.WithTimeProvider(clock)));
        var withoutClock = new ServiceCollection();
        withoutClock.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));

        using var withClockProvider = withClock.BuildServiceProvider();
        using var withoutClockProvider = withoutClock.BuildServiceProvider();

        withClockProvider.GetRequiredService<HoldemOptions>().TimeProvider.Should().BeSameAs(clock);
        withoutClockProvider.GetRequiredService<HoldemOptions>().TimeProvider.Should().BeSameAs(TimeProvider.System);
    }

    [TestMethod]
    public void IHoldemTournament_ResolvesAsAnInternalSingleton()
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));
        using var provider = services.BuildServiceProvider();

        var tournament = provider.GetRequiredService<IHoldemTournament>();

        tournament.Should().BeSameAs(provider.GetRequiredService<IHoldemTournament>());
        tournament.GetType().IsPublic.Should().BeFalse("the implementation is reached only through IHoldemTournament");
    }

    private static async Task<TournamentData> CreateAsync(IHoldemTournament tournament, TournamentOptionData options)
    {
        var result = await tournament.ApplyAsync(TournamentAction.CreateTournament(options, _tournamentId));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static async Task<TournamentData> RegisterAsync(IHoldemTournament tournament, string name)
    {
        var result = await tournament.ApplyAsync(TournamentAction.Register(_tournamentId, name));
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors));
        return result.Value;
    }

    private static async Task AssertVersionAsync(IHoldemTournament tournament, int version)
        => (await tournament.ApplyAsync(TournamentAction.GetTournament(_tournamentId))).Value.Version.Should().Be(version);

    private static async Task SetStateAsync(IDbContextFactory<HoldemDbContext> contextFactory, TournamentState state)
    {
        using var dbContext = await contextFactory.CreateDbContextAsync();
        var row = await dbContext.Set<Tournament>().SingleAsync(candidate => candidate.Id == _tournamentId);
        row.State = (byte)state;
        await dbContext.SaveChangesAsync();
    }

    private static void AssertFailed(HoldemResult<TournamentData> result, HoldemErrorCode code)
    {
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record UnknownTournamentAction(Guid TournamentId) : TournamentAction(TournamentId);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    }
}
