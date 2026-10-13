using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.AspNetCore.Tests;

/// <summary>An in-process ASP.NET Core host over the engine, with its own InMemory database.</summary>
internal sealed class TestApiHost : IAsyncDisposable
{
    private TestApiHost(WebApplication app)
    {
        App = app;
        Client = app.GetTestClient();
        Engine = app.Services.GetRequiredService<IHoldemTable>();
    }

    public WebApplication App { get; }

    public HttpClient Client { get; }

    /// <summary>The engine of the host, for seeding games directly.</summary>
    public IHoldemTable Engine { get; }

    /// <param name="map">Maps the endpoints; <c>app.MapHoldemEngineApi()</c> when omitted.</param>
    /// <param name="configure">Adjusts the builder before the app is built.</param>
    public static async Task<TestApiHost> StartAsync(Action<WebApplication>? map = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["TabletopCardEngine:GamePlayersMaxCount"] = "6" });
        builder.Services.AddHoldemEngine(
            builder.Configuration.GetSection("TabletopCardEngine"),
            db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")));
        configure?.Invoke(builder);

        var app = builder.Build();
        await app.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        if (map is null)
        {
            app.MapHoldemEngineApi();
        }
        else
        {
            map(app);
        }

        await app.StartAsync();
        return new TestApiHost(app);
    }

    /// <summary>Creates and starts a heads-up game (1/2, stacks 100/100), which leaves it at version 2.</summary>
    public async Task<Guid> SeedHeadsUpGameAsync()
    {
        var created = await Engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
        {
            GameType = GameType.Holdem,
            BettingStructure = BettingStructure.NoLimit,
            MaxPlayersCount = 2,
            InitialSmallBlind = 1,
            InitialDealerSeatNo = 1,
        }));
        var gameId = created.Value.GameId;
        await Engine.ApplyAsync(HoldemAction.StartGame(gameId,
            [new PlayerJoinData("a", 1, 100), new PlayerJoinData("b", 2, 100)]));
        return gameId;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}
