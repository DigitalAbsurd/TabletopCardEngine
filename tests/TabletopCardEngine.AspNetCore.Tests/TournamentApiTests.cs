using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TabletopCardEngine.AspNetCore.Tests;

[TestClass]
public sealed class TournamentApiTests
{
    private const string Base = "/api/holdem";

    private const string TwoPlayerOptions = """
        {
          "options": {
            "tableFormat": "HeadsUp",
            "maxPlayersPerTable": 2,
            "startingStack": 100,
            "buyIn": 10,
            "levels": [ { "smallBlind": 1, "ante": 0, "duration": "00:10:00" } ],
            "payouts": [70, 30]
          }
        }
        """;

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static Task<HttpResponseMessage> SendAsync(TestApiHost host, HttpMethod method, string path, string? json = null, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return host.Client.SendAsync(request);
    }

    private static async Task<Guid> CreateAsync(TestApiHost host, string json = TwoPlayerOptions)
    {
        var response = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments", json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadJsonAsync(response)).GetProperty("tournamentId").GetGuid();
    }

    private static async Task<JsonElement> RegisterAsync(TestApiHost host, Guid tournamentId, string name)
    {
        var response = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/entries", $"{{\"name\":\"{name}\"}}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> StartAsync(TestApiHost host, Guid tournamentId)
    {
        var response = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/start", "{\"seatDrawSeed\":7}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadJsonAsync(response);
    }

    [TestMethod]
    public async Task Tournament_CreateRegisterAndUnregister_WorksOverHttp()
    {
        await using var host = await TestApiHost.StartAsync();

        var created = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments", TwoPlayerOptions);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.ETag!.Tag.Should().Be("\"1\"");
        var tournamentId = (await ReadJsonAsync(created)).GetProperty("tournamentId").GetGuid();
        created.Headers.Location!.PathAndQuery.Should().Be($"{Base}/tournaments/{tournamentId}");

        var got = await host.Client.GetAsync($"{Base}/tournaments/{tournamentId}");
        got.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(got);
        body.GetProperty("state").GetString().Should().Be("Registering");
        body.GetProperty("options").GetProperty("levels")[0].GetProperty("duration").GetString().Should().Be("00:10:00");

        await RegisterAsync(host, tournamentId, "Ann");
        await RegisterAsync(host, tournamentId, "Bob");
        var three = await RegisterAsync(host, tournamentId, "Cy");
        three.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("entryNo").GetInt32()).Should().Equal(1, 2, 3);
        three.GetProperty("entries")[0].GetProperty("state").GetString().Should().Be("Registered");

        var duplicate = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/entries", "{\"name\":\"ann\"}");
        duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        (await ReadJsonAsync(duplicate)).GetProperty("code").GetString().Should().Be("InvalidEntry");

        var removed = await SendAsync(host, HttpMethod.Delete, $"{Base}/tournaments/{tournamentId}/entries/2");
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(removed)).GetProperty("entries").EnumerateArray()
            .Select(e => e.GetProperty("entryNo").GetInt32()).Should().Equal(1, 3);
    }

    [TestMethod]
    public async Task Tournament_Create_TwiceWithTheSameId_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var id = Guid.NewGuid();
        var json = TwoPlayerOptions.Replace("\"options\"", $"\"tournamentId\": \"{id}\", \"options\"", StringComparison.Ordinal);

        (await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments", json)).StatusCode.Should().Be(HttpStatusCode.Created);
        var again = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments", json);

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(again)).GetProperty("code").GetString().Should().Be("TournamentAlreadyExists");
    }

    [TestMethod]
    public async Task Tournament_Start_OpensTablesAsGamesManagedByTheTournament()
    {
        await using var host = await TestApiHost.StartAsync();
        var tournamentId = await CreateAsync(host);
        await RegisterAsync(host, tournamentId, "Ann");
        await RegisterAsync(host, tournamentId, "Bob");

        var started = await StartAsync(host, tournamentId);

        started.GetProperty("state").GetString().Should().Be("Running");
        var gameId = started.GetProperty("tables")[0].GetProperty("gameId").GetGuid();
        gameId.Should().NotBeEmpty();

        var hands = await SendAsync(host, HttpMethod.Post, $"{Base}/games/{gameId}/hands", "{}");
        hands.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(hands)).GetProperty("code").GetString().Should().Be("ManagedByTournament");

        var play = await SendAsync(host, HttpMethod.Post, $"{Base}/games/{gameId}/actions", "{\"actionType\":\"Call\"}");
        play.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Tournament_HeadsUpPlayedOverHttp_FinishesWithPlacesAndPrizes()
    {
        await using var host = await TestApiHost.StartAsync();
        var tournamentId = await CreateAsync(host);
        await RegisterAsync(host, tournamentId, "Ann");
        await RegisterAsync(host, tournamentId, "Bob");
        var tournament = await StartAsync(host, tournamentId);
        var gameId = tournament.GetProperty("tables")[0].GetProperty("gameId").GetGuid();

        for (var hand = 0; hand < 60 && tournament.GetProperty("state").GetString() != "Finished"; hand++)
        {
            for (var step = 0; step < 10; step++)
            {
                var game = await ReadJsonAsync(await host.Client.GetAsync($"{Base}/games/{gameId}"));
                var handData = game.GetProperty("handData");
                if (handData.GetProperty("street").GetString() == "Finished")
                {
                    break;
                }

                var offered = handData.GetProperty("evaluations").EnumerateArray()
                    .Select(e => e.GetProperty("playerActionType").GetString()).ToList();
                var action = offered.Contains("Raise") ? "AllIn" : offered.Contains("Call") ? "Call" : "Check";
                var played = await SendAsync(host, HttpMethod.Post, $"{Base}/games/{gameId}/actions", $"{{\"actionType\":\"{action}\"}}");
                played.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            var dealt = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/tables/1/deal");
            dealt.StatusCode.Should().Be(HttpStatusCode.OK);
            tournament = await ReadJsonAsync(dealt);
        }

        tournament.GetProperty("state").GetString().Should().Be("Finished");
        var entries = tournament.GetProperty("entries").EnumerateArray().ToList();
        entries.Select(e => e.GetProperty("place").GetInt32()).OrderBy(p => p).Should().Equal(1, 2);
        entries.Sum(e => e.GetProperty("prize").GetDecimal()).Should().Be(tournament.GetProperty("prizePool").GetDecimal());
        tournament.GetProperty("prizePool").GetDecimal().Should().Be(20m);
        entries.Single(e => e.GetProperty("place").GetInt32() == 1).GetProperty("state").GetString().Should().Be("Winner");
    }

    [TestMethod]
    public async Task Tournament_Deal_WithStaleIfMatch_Returns412()
    {
        await using var host = await TestApiHost.StartAsync();
        var tournamentId = await CreateAsync(host);
        await RegisterAsync(host, tournamentId, "Ann");
        await RegisterAsync(host, tournamentId, "Bob");
        await StartAsync(host, tournamentId);

        var response = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/tables/1/deal", ifMatch: "\"1\"");

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("Conflict");
    }

    [TestMethod]
    public async Task Tournament_Unknown_Returns404()
    {
        await using var host = await TestApiHost.StartAsync();

        var response = await host.Client.GetAsync($"{Base}/tournaments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("TournamentNotFound");
    }

    [TestMethod]
    public async Task Tournament_Deal_DuringAHand_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var tournamentId = await CreateAsync(host);
        await RegisterAsync(host, tournamentId, "Ann");
        await RegisterAsync(host, tournamentId, "Bob");
        await StartAsync(host, tournamentId);

        var response = await SendAsync(host, HttpMethod.Post, $"{Base}/tournaments/{tournamentId}/tables/1/deal");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("PreviousHandNotEnded");
    }
}
