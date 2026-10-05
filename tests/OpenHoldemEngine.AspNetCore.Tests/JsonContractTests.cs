using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenHoldemEngine.AspNetCore.Json;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.InMemory;
using OpenHoldemEngine.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>
/// Tests for JSON serialization of engine types (enums as names, cards as strings).
/// </summary>
[TestClass]
public sealed class JsonContractTests
{
    private static readonly JsonSerializerOptions _options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        HoldemJson.AddConverters(options.Converters);
        return options;
    }

    [TestMethod]
    public async Task GameData_SerializesEnumsAsNamesAndCardsAsStrings()
    {
        using var host = BuildHost(new Dictionary<string, string?>(), null);
        await StartAsync(host);

        var engine = host.Services.GetRequiredService<HoldemEngine>();
        var gameId = Guid.NewGuid();

        // Create a game
        var createResult = await engine.ApplyHoldemActionAsync(
            HoldemAction.CreateGame(new GameOptionData { GameType = GameType.Holdem, BettingStructure = BettingStructure.NoLimit, MaxPlayersCount = 2, InitialSmallBlind = 1, InitialDealerSeatNo = 1 }, gameId),
            CancellationToken.None);

        // Start the game with two players
        var startResult = await engine.ApplyHoldemActionAsync(
            HoldemAction.StartGame(gameId, new[]
            {
                new PlayerJoinData("a", 1, 100),
                new PlayerJoinData("b", 2, 100)
            }),
            CancellationToken.None);

        var gameData = startResult.Value;
        var json = JsonSerializer.Serialize(gameData, _options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Verify enum serialized as name
        root.GetProperty("options").GetProperty("gameType").GetString().Should().Be("Holdem");
        root.GetProperty("options").GetProperty("bettingStructure").GetString().Should().Be("NoLimit");

        // Verify street serialized as name
        root.GetProperty("handData").GetProperty("street").GetString().Should().Be("PreFlop");

        // Verify hand action type serialized as name
        var seat1 = root.GetProperty("handData").GetProperty("seats")[0];
        seat1.GetProperty("lastAction").GetProperty("actionType").GetString().Should().Be("SmallBlind");

        // Verify cards serialized as strings matching pattern
        var hole = seat1.GetProperty("hole");
        hole.GetArrayLength().Should().Be(2);
        foreach (var cardElement in hole.EnumerateArray())
        {
            var cardString = cardElement.GetString();
            cardString.Should().MatchRegex(@"^[2-9tjqka][cdhs]$");
        }

        // Verify evaluations have player action types as names
        var evaluations = root.GetProperty("handData").GetProperty("evaluations");
        foreach (var eval in evaluations.EnumerateArray())
        {
            eval.GetProperty("playerActionType").GetString().Should().NotBeNull();
            eval.GetProperty("playerActionType").GetString().Should().MatchRegex(@"^[A-Z][a-zA-Z]*$");
        }

        // Verify smallBlind and bigBlind are present as JSON numbers
        root.GetProperty("handData").GetProperty("smallBlind").GetDecimal().Should().Be(1M);
        root.GetProperty("handData").GetProperty("bigBlind").GetDecimal().Should().Be(2M);

        await host.StopAsync();
    }

    [TestMethod]
    public void PlayerActionType_ReadsNames_AndRefusesNumbers()
    {
        // Reading a name should work
        var json = "\"Raise\"";
        var result = JsonSerializer.Deserialize<PlayerActionType>(json, _options);
        result.Should().Be(PlayerActionType.Raise);

        // Reading a number should throw
        var numberJson = "6";
        var act = () => JsonSerializer.Deserialize<PlayerActionType>(numberJson, _options);
        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Card_ReadsAnyCase_AndRefusesGarbage()
    {
        // Reading uppercase should work
        var upperJson = "\"AH\"";
        var result = JsonSerializer.Deserialize<Card>(upperJson, _options);
        result.Should().Be(new Card(CardType.Ace, CardSuit.Heart));

        // Writing should be lowercase canonical
        var serialized = JsonSerializer.Serialize(result, _options);
        serialized.Should().Be("\"ah\"");

        // Reading garbage should throw
        var garbageJson = "\"zz\"";
        var act1 = () => JsonSerializer.Deserialize<Card>(garbageJson, _options);
        act1.Should().Throw<JsonException>();

        // Reading a number should throw
        var numberJson = "12";
        var act2 = () => JsonSerializer.Deserialize<Card>(numberJson, _options);
        act2.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Converters_LeaveOtherEnumsAlone()
    {
        // DayOfWeek should still serialize as its numeric value
        var dayOfWeek = DayOfWeek.Monday;
        var serialized = JsonSerializer.Serialize(dayOfWeek, _options);
        serialized.Should().Be("1");
    }

    [TestMethod]
    public void AddHoldemEngine_RegistersTheConverters()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Use the hosted overload that registers converters
        services.AddHoldemEngine(
            config.GetSection("HoldemEngine"),
            dbOptions => dbOptions.UseHoldemInMemory(Guid.NewGuid().ToString("N")));

        var sp = services.BuildServiceProvider();

        // Check that the converters are registered
        var jsonOptions = sp.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value;
        var hasCardConverter = jsonOptions.SerializerOptions.Converters.OfType<CardJsonConverter>().Any();
        hasCardConverter.Should().BeTrue("CardJsonConverter should be registered");

        var hasEnumConverter = jsonOptions.SerializerOptions.Converters.OfType<JsonStringEnumConverter<PlayerActionType>>().Any();
        hasEnumConverter.Should().BeTrue("JsonStringEnumConverter for enums should be registered");

        // Check that the marker is registered
        var marker = sp.GetService<HoldemEngineApiMarker>();
        marker.Should().NotBeNull("HoldemEngineApiMarker should be registered");
    }

    private static IHost BuildHost(Dictionary<string, string?> settings, Action<HoldemEngineOptionsBuilder>? configure)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(settings.ToDictionary(p => "HoldemEngine:" + p.Key, p => p.Value));
        builder.Services.AddHoldemEngine(
            builder.Configuration.GetSection("HoldemEngine"),
            db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")),
            configure);
        return builder.Build();
    }

    private static async Task StartAsync(IHost host)
    {
        await host.Services.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        await host.StartAsync();
    }
}
