using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.Tests;

[TestClass]
public sealed class HoldemEngineTests
{
    private ServiceProvider _serviceProvider = null!;
    private HoldemEngine _holdemEngine = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        AssertionOptions.AssertEquivalencyUsing(opt =>
        {
            opt.ComparingRecordsByValue();
            opt.IgnoringCyclicReferences();
            return opt;
        });
        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(dbOptionsAction: opts => opts.UseInMemoryDatabase(nameof(HoldemDbContext)));
        _serviceProvider = services.BuildServiceProvider();
        _holdemEngine = _serviceProvider.GetRequiredService<HoldemEngine>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _serviceProvider.Dispose();
        _holdemEngine = null!;
    }

    [TestMethod]
    public async Task GetGameContextAsync_NewGameDetail()
    {
        var gameId = Guid.NewGuid();
        var expected = new GameContext(gameId, ActionDetail: new HoldemActionDetailStartGame());

        var actual = await _holdemEngine.GetGameContextAsync(gameId);

        actual.Should().BeEquivalentTo(expected);
    }
}
