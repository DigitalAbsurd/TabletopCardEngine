using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class HoldemBuilderTests
{
    [TestMethod]
    public void AddHoldem_Twice_Throws()
    {
        var oneBuilder = new ServiceCollection();
        var twiceInOneBuilder = () => oneBuilder.AddTabletopCardEngine(tce => tce
            .AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")))
            .AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));

        twiceInOneBuilder.Should().Throw<InvalidOperationException>();

        var twoBuilders = new ServiceCollection();
        twoBuilders.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));
        var secondBuilder = () => twoBuilders.AddTabletopCardEngine(tce =>
            tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N"))));

        secondBuilder.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void AddTabletopCardEngine_NullConfigure_Throws()
    {
        var services = new ServiceCollection();

        var register = () => services.AddTabletopCardEngine(null!);

        register.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("configure");
    }

    [TestMethod]
    public void AddTabletopCardEngine_WithoutAGame_RegistersNoTable()
    {
        var services = new ServiceCollection();

        services.AddTabletopCardEngine(_ => { });
        using var provider = services.BuildServiceProvider();

        provider.GetService<IHoldemTable>().Should().BeNull();
    }
}
