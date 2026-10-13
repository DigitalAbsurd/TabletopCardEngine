using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.Sqlite;
using TabletopCardEngine.Tests.Persistence;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class ConcurrencyTests
{
    private static readonly PlayerJoinData[] _seats =
    [
        new("A", 1, 100),
        new("B", 2, 100),
        new("C", 3, 100),
    ];

    private static readonly GameOptionData _options = new() { MaxPlayersCount = 6, InitialSmallBlind = 1, InitialDealerSeatNo = 1 };

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task ExpectedVersion_Stale_ReturnsConflict(TestDatabaseKind database)
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory, database: database);
        var gameId = Guid.NewGuid();

        var created = await engine.ApplyAsync(HoldemAction.CreateGame(_options, gameId));
        created.Value.Version.Should().Be(1);
        var started = await engine.ApplyAsync(HoldemAction.StartGame(gameId, _seats));
        var staleVersion = started.Value.Version;
        staleVersion.Should().Be(2);

        var played = await engine.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Call) with { ExpectedVersion = staleVersion });
        played.IsSuccess.Should().BeTrue();
        played.Value.Version.Should().Be(staleVersion + 1);
        var rowsBefore = await CountActionsAsync(contextFactory, gameId);

        var stale = await engine.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold) with { ExpectedVersion = staleVersion });

        stale.IsSuccess.Should().BeFalse();
        stale.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.Conflict);
        (await CountActionsAsync(contextFactory, gameId)).Should().Be(rowsBefore);
        var reloaded = await engine.ApplyAsync(HoldemAction.GetGame(gameId));
        reloaded.Value.Version.Should().Be(staleVersion + 1);
        reloaded.Value.ShouldAsJToken().BeEquivalentToAsJToken(played.Value);
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task GetGame_Twice_KeepsTheVersion(TestDatabaseKind database)
    {
        using var host = TestEngineHost.Setup(out var engine, out _, database: database);
        var gameId = Guid.NewGuid();
        await engine.ApplyAsync(HoldemAction.CreateGame(_options, gameId));

        var first = await engine.ApplyAsync(HoldemAction.GetGame(gameId));
        var second = await engine.ApplyAsync(HoldemAction.GetGame(gameId));

        second.Value.Version.Should().Be(first.Value.Version).And.Be(1);
    }

    [TestMethod]
    public async Task TwoWriters_SecondGetsConflict()
    {
        // Two writers over one SQLite *file*: a shared :memory: connection would serialise them and prove nothing.
        var fileName = Path.Combine(Path.GetTempPath(), $"tce-concurrency-{Guid.NewGuid():N}.db3");
        try
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using var provider = BuildHost(opts => opts.UseHoldemSqlite($"Filename='{fileName}'"));
            await RaceTwoWritersAsync(provider);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(fileName);
        }
    }

    [TestMethod]
    public async Task TwoWriters_SecondGetsConflict_InMemory()
    {
        // One container, one named store: both writers see each other's saves, as two processes would on a server.
        var databaseName = Guid.NewGuid().ToString("N");
        using var provider = BuildHost(opts => opts.UseHoldemInMemory(databaseName));
        await RaceTwoWritersAsync(provider);
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task TwoWriters_SecondGetsConflict_Server(TestDatabaseKind kind)
    {
        // A server serialises nothing, so two writers in one container race as two processes would.
        var configure = TestDatabase.Configure(kind, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = BuildHost(configure);
        await RaceTwoWritersAsync(provider);
    }

    /// <summary>
    /// Both writers load the same version and handle the same action; only then does the first save, and
    /// the second after it. The second must get <see cref="HoldemErrorCode.Conflict"/> and persist nothing.
    /// </summary>
    private static async Task RaceTwoWritersAsync(ServiceProvider provider)
    {
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var contextFactory = provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        var engine = (HoldemTable)provider.GetRequiredService<IHoldemTable>();
        var gameId = Guid.NewGuid();
        await engine.ApplyAsync(HoldemAction.CreateGame(_options, gameId));
        var started = await engine.ApplyAsync(HoldemAction.StartGame(gameId, _seats));
        var rowsBefore = await CountActionsAsync(contextFactory, gameId);

        var loadedFirst = new TaskCompletionSource();
        var loadedSecond = new TaskCompletionSource();
        var firstSaved = new TaskCompletionSource();
        var action = HoldemAction.Play(gameId, PlayerActionType.Call);

        var first = engine.ApplyWithHookAsync(action, async () =>
        {
            loadedFirst.SetResult();
            await loadedSecond.Task;
        }, CancellationToken.None);
        var second = engine.ApplyWithHookAsync(action, async () =>
        {
            loadedSecond.SetResult();
            await loadedFirst.Task;
            await firstSaved.Task;
        }, CancellationToken.None);

        var firstResult = await first;
        firstSaved.SetResult();
        var secondResult = await second;

        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Version.Should().Be(started.Value.Version + 1);
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.Conflict);
        (await CountActionsAsync(contextFactory, gameId)).Should().Be(rowsBefore + 1);
    }

    private static ServiceProvider BuildHost(Action<DbContextOptionsBuilder> configureDatabase)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(dbOptionsAction: configureDatabase, configure: _ => { }));
        return services.BuildServiceProvider();
    }

    private static async Task<int> CountActionsAsync(IDbContextFactory<HoldemDbContext> contextFactory, Guid gameId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.Set<HandAction>().CountAsync(action => action.GameId == gameId);
    }
}
