using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TabletopCardEngine.AspNetCore.Tests;

[TestClass]
public sealed class GamePlayEndpointTests
{
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [TestMethod]
    public async Task Play_Call_Returns200AndBumpsTheETag()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Content = new StringContent("{\"actionType\":\"Call\"}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"3\"");
    }

    [TestMethod]
    public async Task Play_IllegalCheck_Returns422()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Content = new StringContent("{\"actionType\":\"Check\"}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("IllegalAction");

        // Verify that the game is unchanged
        var getResponse = await host.Client.GetAsync($"/api/holdem/games/{gameId}");
        getResponse.Headers.ETag!.Tag.Should().Be("\"2\"");
        var gameData = await ReadJsonAsync(getResponse);
        gameData.GetProperty("handData").GetProperty("nextPlayerSeatNo").GetInt32().Should().Be(1);
    }

    [TestMethod]
    public async Task Play_RaiseBelowTheMinimum_Returns422()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Content = new StringContent("{\"actionType\":\"Raise\",\"size\":2.5}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("IllegalAction");
    }

    [TestMethod]
    public async Task Play_AllIn_Returns200()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Content = new StringContent("{\"actionType\":\"AllIn\"}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        var seats = body.GetProperty("handData").GetProperty("seats").EnumerateArray().ToList();
        var seat1 = seats.First(s => s.GetProperty("seatNo").GetInt32() == 1);
        seat1.GetProperty("lastAction").GetProperty("actionType").GetString().Should().Be("AllIn");
    }

    [TestMethod]
    public async Task Play_NumericActionType_Returns400()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Content = new StringContent("{\"actionType\":6}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Play_StaleIfMatch_Returns412AndChangesNothing()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        request.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        request.Content = new StringContent("{\"actionType\":\"Call\"}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        // Verify that the game is unchanged
        var getResponse = await host.Client.GetAsync($"/api/holdem/games/{gameId}");
        getResponse.Headers.ETag!.Tag.Should().Be("\"2\"");
    }

    [TestMethod]
    public async Task StartHand_MidHand_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/hands");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("PreviousHandNotEnded");
    }

    [TestMethod]
    public async Task StartHand_AfterAFold_DealsHandTwo()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        // Seat 1 (small blind) folds
        using var foldRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        foldRequest.Content = new StringContent("{\"actionType\":\"Fold\"}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(foldRequest);

        // Deal hand 2 with no body
        using var startHandRequest1 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/hands");
        startHandRequest1.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response1 = await host.Client.SendAsync(startHandRequest1);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response1)).GetProperty("handData").GetProperty("no").GetInt32().Should().Be(2);

        // Test with a second game with explicit empty joinSeats
        var gameId2 = await host.SeedHeadsUpGameAsync();
        using var foldRequest2 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId2}/actions");
        foldRequest2.Content = new StringContent("{\"actionType\":\"Fold\"}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(foldRequest2);

        using var startHandRequest2 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId2}/hands");
        startHandRequest2.Content = new StringContent("{\"joinSeats\":[]}", Encoding.UTF8, "application/json");
        var response2 = await host.Client.SendAsync(startHandRequest2);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response2)).GetProperty("handData").GetProperty("no").GetInt32().Should().Be(2);
    }

    [TestMethod]
    public async Task FinishGame_MidHand_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/finish");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be("PreviousHandNotEnded");
    }

    [TestMethod]
    public async Task FinishGame_BetweenHands_Returns200()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        // Seat 1 folds
        using var foldRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        foldRequest.Content = new StringContent("{\"actionType\":\"Fold\"}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(foldRequest);

        // Finish the game
        using var finishRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/finish");
        var finishResponse = await host.Client.SendAsync(finishRequest);

        finishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(finishResponse)).GetProperty("isFinished").GetBoolean().Should().BeTrue();

        // Actions should give 409 GameFinished
        using var actionsRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        actionsRequest.Content = new StringContent("{\"actionType\":\"Call\"}", Encoding.UTF8, "application/json");
        var actionsResponse = await host.Client.SendAsync(actionsRequest);

        actionsResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(actionsResponse)).GetProperty("code").GetString().Should().Be("GameFinished");

        // GET should still give 200 with isFinished true
        var getResponse = await host.Client.GetAsync($"/api/holdem/games/{gameId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(getResponse)).GetProperty("isFinished").GetBoolean().Should().BeTrue();
    }

    [TestMethod]
    public async Task Routes_UnknownGame_Return404()
    {
        await using var host = await TestApiHost.StartAsync();
        var unknownId = Guid.NewGuid();

        // hands
        using var handsRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{unknownId}/hands");
        handsRequest.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var handsResponse = await host.Client.SendAsync(handsRequest);

        handsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(handsResponse)).GetProperty("code").GetString().Should().Be("GameNotFound");

        // actions
        using var actionsRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{unknownId}/actions");
        actionsRequest.Content = new StringContent("{\"actionType\":\"Call\"}", Encoding.UTF8, "application/json");
        var actionsResponse = await host.Client.SendAsync(actionsRequest);

        actionsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(actionsResponse)).GetProperty("code").GetString().Should().Be("GameNotFound");

        // finish
        using var finishRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{unknownId}/finish");
        var finishResponse = await host.Client.SendAsync(finishRequest);

        finishResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(finishResponse)).GetProperty("code").GetString().Should().Be("GameNotFound");
    }

    [TestMethod]
    public async Task LeaveSeat_BetweenHands_Returns200()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        // Seat 1 folds to end the hand
        using var foldRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        foldRequest.Content = new StringContent("{\"actionType\":\"Fold\"}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(foldRequest);

        // Leave seat 1
        using var leaveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/seats/1/leave");
        var leaveResponse = await host.Client.SendAsync(leaveRequest);

        leaveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var game = await ReadJsonAsync(leaveResponse);
        var seats = game.GetProperty("handData").GetProperty("seats").EnumerateArray().ToList();
        seats.Should().HaveCount(2);
        var activeSeat = seats.First(s => !s.GetProperty("hasLeft").GetBoolean());
        activeSeat.GetProperty("seatNo").GetInt32().Should().Be(2);
    }

    [TestMethod]
    public async Task LeaveSeat_MidHand_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        // Try to leave seat 1 mid-hand
        using var leaveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/seats/1/leave");
        var leaveResponse = await host.Client.SendAsync(leaveRequest);

        leaveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(leaveResponse)).GetProperty("code").GetString().Should().Be("PreviousHandNotEnded");
    }

    [TestMethod]
    public async Task LeaveSeat_EmptySeat_Returns422()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = await host.SeedHeadsUpGameAsync();

        // Seat 1 folds to end the hand
        using var foldRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/actions");
        foldRequest.Content = new StringContent("{\"actionType\":\"Fold\"}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(foldRequest);

        // Try to leave an empty seat
        using var leaveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/seats/3/leave");
        var leaveResponse = await host.Client.SendAsync(leaveRequest);

        leaveResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        (await ReadJsonAsync(leaveResponse)).GetProperty("code").GetString().Should().Be("InvalidSeat");
    }

    [TestMethod]
    public async Task LeaveSeat_UnknownGame_Returns404()
    {
        await using var host = await TestApiHost.StartAsync();
        var unknownId = Guid.NewGuid();

        using var leaveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{unknownId}/seats/1/leave");
        var leaveResponse = await host.Client.SendAsync(leaveRequest);

        leaveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadJsonAsync(leaveResponse)).GetProperty("code").GetString().Should().Be("GameNotFound");
    }
}
