using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine.Tests;

[TestClass]
public sealed class HoldemEngineTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        AssertionOptions.AssertEquivalencyUsing(opt =>
        {
            opt.ComparingRecordsByValue();
            opt.IgnoringCyclicReferences();
            return opt;
        });
    }

    [TestMethod]
    public async Task GetGameContextAsync_NewGameDetail()
    {
        var minPlayersCount = 4;
        var maxPlayersCount = 8;
        var gameId = Guid.NewGuid();
        using var holdemServices = SetupHoldemEngineServices(out var holdemEngine, 
            configureSettings: settings =>
            {
                settings.GamePlayersMinCount = minPlayersCount;
                settings.GamePlayersMaxCount = maxPlayersCount;
            });
        var expected = new GameContext(gameId, ActionDetail: new HoldemActionDetailStartGame(minPlayersCount, maxPlayersCount));

        var actual = await holdemEngine.GetGameContextAsync(gameId);

        actual.Should().BeEquivalentTo(expected);
    }

    private IDisposable SetupHoldemEngineServices(out HoldemEngine holdemEngine, Action<HoldemEngineSettings> configureSettings = null!)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseInMemoryDatabase(nameof(HoldemDbContext)),
            configure: opts =>
            {
                if (configureSettings != null)
                    opts.WithSettings(configureSettings);
            }
            );
        var serviceProvider = services.BuildServiceProvider();
        holdemEngine = serviceProvider.GetRequiredService<HoldemEngine>();
        return serviceProvider;
    }
}
