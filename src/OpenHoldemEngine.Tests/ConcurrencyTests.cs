using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

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
    public async Task ExpectedVersion_Stale_ReturnsConflict()
    {
        using var host = TestEngineHost.Setup(out var engine, out var contextFactory);
        var gameId = Guid.NewGuid();

        var created = await engine.ApplyHoldemActionAsync(HoldemAction.CreateGame(_options, gameId));
        created.Value.Version.Should().Be(1);
        var started = await engine.ApplyHoldemActionAsync(HoldemAction.StartGame(gameId, _seats));
        var staleVersion = started.Value.Version;
        staleVersion.Should().Be(2);

        var played = await engine.ApplyHoldemActionAsync(HoldemAction.Play(gameId, PlayerActionType.Call) with { ExpectedVersion = staleVersion });
        played.IsSuccess.Should().BeTrue();
        played.Value.Version.Should().Be(staleVersion + 1);
        var rowsBefore = await CountActionsAsync(contextFactory, gameId);

        var stale = await engine.ApplyHoldemActionAsync(HoldemAction.Play(gameId, PlayerActionType.Fold) with { ExpectedVersion = staleVersion });

        stale.IsSuccess.Should().BeFalse();
        stale.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.Conflict);
        (await CountActionsAsync(contextFactory, gameId)).Should().Be(rowsBefore);
        var reloaded = await engine.ApplyHoldemActionAsync(HoldemAction.GetGame(gameId));
        reloaded.Value.Version.Should().Be(staleVersion + 1);
        reloaded.Value.ShouldAsJToken().BeEquivalentToAsJToken(played.Value);
    }

    [TestMethod]
    public async Task GetGame_Twice_KeepsTheVersion()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var gameId = Guid.NewGuid();
        await engine.ApplyHoldemActionAsync(HoldemAction.CreateGame(_options, gameId));

        var first = await engine.ApplyHoldemActionAsync(HoldemAction.GetGame(gameId));
        var second = await engine.ApplyHoldemActionAsync(HoldemAction.GetGame(gameId));

        second.Value.Version.Should().Be(first.Value.Version).And.Be(1);
    }

    [TestMethod]
    public async Task TwoWriters_SecondGetsConflict()
    {
        // Two writers over one SQLite *file*: a shared :memory: connection would serialise them and prove nothing.
        var fileName = Path.Combine(Path.GetTempPath(), $"ohe-concurrency-{Guid.NewGuid():N}.db3");
        try
        {
            using var provider = BuildFileHost(fileName);
            var contextFactory = provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
            using (var setup = contextFactory.CreateDbContext())
            {
                setup.Database.EnsureCreated();
            }

            var engine = provider.GetRequiredService<HoldemEngine>();
            var gameId = Guid.NewGuid();
            await engine.ApplyHoldemActionAsync(HoldemAction.CreateGame(_options, gameId));
            var started = await engine.ApplyHoldemActionAsync(HoldemAction.StartGame(gameId, _seats));
            var rowsBefore = await CountActionsAsync(contextFactory, gameId);

            var loadedFirst = new TaskCompletionSource();
            var loadedSecond = new TaskCompletionSource();
            var firstSaved = new TaskCompletionSource();
            var action = HoldemAction.Play(gameId, PlayerActionType.Call);

            // Both writers load version 2 and handle the action; only then does the first save, and the second after it.
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
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(fileName);
        }
    }

    private static ServiceProvider BuildFileHost(string fileName)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(dbOptionsAction: opts => opts.UseSqlite($"Filename='{fileName}'"), configure: _ => { });
        return services.BuildServiceProvider();
    }

    private static async Task<int> CountActionsAsync(IDbContextFactory<HoldemDbContext> contextFactory, Guid gameId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.Set<HandAction>().CountAsync(action => action.GameId == gameId);
    }
}
