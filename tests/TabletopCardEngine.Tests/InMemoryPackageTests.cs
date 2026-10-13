using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The <c>TabletopCardEngine.InMemory</c> package: its databases have no migrations, no versioning,
/// and no relational drivers. It is for prototyping, testing and isolated unit tests only.
/// </summary>
[TestClass]
public sealed class InMemoryPackageTests
{
    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    private static readonly string[] _relationalDrivers =
    [
        "Microsoft.Data.Sqlite",
        "SQLitePCLRaw",
        "Npgsql",
        "Microsoft.Data.SqlClient",
    ];

    [TestMethod]
    public async Task InMemory_MigrateAsync_CreatesTheStore()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        using var host = InMemoryHost.Create(databaseName);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        await manager.MigrateAsync();
        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.NotVersioned);
        status.PendingMigrations.Should().BeEmpty();
        status.DatabaseSchemaVersion.Should().BeNull();
        status.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
    }

    [TestMethod]
    public async Task InMemory_PlaysAHeadsUpHand()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        using var host = InMemoryHost.Create(databaseName);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();
        await manager.MigrateAsync();

        var engine = host.GetRequiredService<IHoldemTable>();
        var gameId = Guid.NewGuid();
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        var result = await ApplyAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));

        result.HandData.Should().NotBeNull();
        result.HandData!.NextPlayerSeatNo.Should().Be(0, "the hand is over when nobody's turn it is");
        var nets = result.HandData!.Seats.Select(seat => seat.HandResult?.Net ?? 0M).ToList();
        nets.Sum().Should().Be(0M, "chips are conserved");
    }

    [TestMethod]
    public async Task InMemory_WithoutMigrateAsync_StillRuns()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        using var host = InMemoryHost.Create(databaseName);
        var engine = host.GetRequiredService<IHoldemTable>();

        var gameId = Guid.NewGuid();
        var result = await engine.ApplyAsync(new HoldemActionCreateGame(gameId, _options));

        result.IsSuccess.Should().BeTrue("InMemory does not require MigrateAsync");
    }

    [TestMethod]
    public async Task InMemory_DetectsAConcurrencyConflict()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        using var host = InMemoryHost.Create(databaseName);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();
        await manager.MigrateAsync();

        var engine = host.GetRequiredService<IHoldemTable>();
        var gameId = Guid.NewGuid();
        var created = await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));

        var result = await engine.ApplyAsync(new HoldemActionStartGame(gameId, _joinSeats)
        {
            ExpectedVersion = created.Version - 1,
        });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == HoldemErrorCode.Conflict);
    }

    [TestMethod]
    public void InMemoryPackage_PullsNoRelationalDriver()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine.InMemory", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Should().Contain(library => library.StartsWith("Microsoft.EntityFrameworkCore.InMemory/", StringComparison.OrdinalIgnoreCase));
        libraries.Where(library => _relationalDrivers.Any(driver => library.StartsWith(driver, StringComparison.OrdinalIgnoreCase)))
            .Should().BeEmpty();
    }

    private static async Task<GameData> ApplyAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return result.Value;
    }

    /// <summary>
    /// A container configured the way a consumer of the package configures it.
    /// </summary>
    private sealed class InMemoryHost : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private InMemoryHost(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        internal static InMemoryHost Create(string databaseName)
        {
            var services = new ServiceCollection();
            services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemInMemory(databaseName)));
            return new InMemoryHost(services.BuildServiceProvider());
        }

        internal T GetRequiredService<T>() where T : notnull
            => _serviceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
