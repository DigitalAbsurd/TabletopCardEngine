using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenHoldemEngine.AspNetCore.Tests;

[TestClass]
public sealed class GameSetupEndpointTests
{
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [TestMethod]
    public async Task CreateGame_EmptyBody_Returns201WithLocationAndETag()
    {
        await using var host = await TestApiHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.PathAndQuery.Should().MatchRegex(@"/api/holdem/games/[a-f0-9\-]{36}$");
        response.Headers.ETag!.Tag.Should().Be("\"1\"");
        var body = await ReadJsonAsync(response);
        body.GetProperty("options").GetProperty("maxPlayersCount").GetInt32().Should().Be(6);
        body.GetProperty("options").GetProperty("gameType").GetString().Should().Be("Holdem");
    }

    [TestMethod]
    public async Task CreateGame_WithIdAndOptions()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = Guid.NewGuid();

        var json = $"{{\"gameId\":\"{gameId}\",\"options\":{{\"maxPlayersCount\":2,\"initialSmallBlind\":0.5}}}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        body.GetProperty("gameId").GetGuid().Should().Be(gameId);

        // The same request again should give 409
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request2.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response2 = await host.Client.SendAsync(request2);

        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadJsonAsync(response2);
        problem.GetProperty("code").GetString().Should().Be("GameAlreadyExists");
    }

    [TestMethod]
    public async Task CreateGame_InvalidOptions_Returns422()
    {
        await using var host = await TestApiHost.StartAsync();

        var json = "{\"options\":{\"maxPlayersCount\":9}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("InvalidGameOptions");
    }

    [TestMethod]
    public async Task CreateGame_UnknownEnumName_Returns400()
    {
        await using var host = await TestApiHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request.Content = new StringContent("{\"options\":{\"gameType\":\"Omaha\"}}", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateGame_MalformedJson_Returns400()
    {
        await using var host = await TestApiHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        request.Content = new StringContent("{invalid json", Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task StartGame_HeadsUp_Returns200()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = Guid.NewGuid();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        createRequest.Content = new StringContent($"{{\"gameId\":\"{gameId}\"}}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(createRequest);

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/start");
        startRequest.Content = new StringContent(
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100}]}",
            Encoding.UTF8,
            "application/json");

        var response = await host.Client.SendAsync(startRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"2\"");
        var body = await ReadJsonAsync(response);
        body.GetProperty("handData").GetProperty("no").GetInt32().Should().Be(1);
    }

    [TestMethod]
    public async Task StartGame_Twice_Returns409()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = Guid.NewGuid();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        createRequest.Content = new StringContent($"{{\"gameId\":\"{gameId}\"}}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(createRequest);

        var joinSeats = "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100}]}";

        using var startRequest1 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/start");
        startRequest1.Content = new StringContent(joinSeats, Encoding.UTF8, "application/json");
        await host.Client.SendAsync(startRequest1);

        using var startRequest2 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/start");
        startRequest2.Content = new StringContent(joinSeats, Encoding.UTF8, "application/json");

        var response = await host.Client.SendAsync(startRequest2);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("GameAlreadyStarted");
    }

    [TestMethod]
    public async Task StartGame_InvalidSeat_Returns422()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = Guid.NewGuid();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        createRequest.Content = new StringContent($"{{\"gameId\":\"{gameId}\",\"options\":{{\"maxPlayersCount\":2}}}}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(createRequest);

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/start");
        startRequest.Content = new StringContent(
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":7,\"buyInAmount\":100}]}",
            Encoding.UTF8,
            "application/json");

        var response = await host.Client.SendAsync(startRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("InvalidSeat");
    }

    [TestMethod]
    public async Task StartGame_UnknownGame_Returns404()
    {
        await using var host = await TestApiHost.StartAsync();

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{Guid.NewGuid()}/start");
        startRequest.Content = new StringContent(
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100}]}",
            Encoding.UTF8,
            "application/json");

        var response = await host.Client.SendAsync(startRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadJsonAsync(response);
        problem.GetProperty("code").GetString().Should().Be("GameNotFound");
    }

    [TestMethod]
    public async Task StartGame_IfMatch()
    {
        await using var host = await TestApiHost.StartAsync();
        var gameId = Guid.NewGuid();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        createRequest.Content = new StringContent($"{{\"gameId\":\"{gameId}\"}}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(createRequest);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId}/start");
        request.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        request.Content = new StringContent(
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100}]}",
            Encoding.UTF8,
            "application/json");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"2\"");

        // Now with a wrong version
        var gameId2 = Guid.NewGuid();
        using var createRequest2 = new HttpRequestMessage(HttpMethod.Post, "/api/holdem/games");
        createRequest2.Content = new StringContent($"{{\"gameId\":\"{gameId2}\"}}", Encoding.UTF8, "application/json");
        await host.Client.SendAsync(createRequest2);

        using var request2 = new HttpRequestMessage(HttpMethod.Post, $"/api/holdem/games/{gameId2}/start");
        request2.Headers.TryAddWithoutValidation("If-Match", "\"5\"");
        request2.Content = new StringContent(
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100}]}",
            Encoding.UTF8,
            "application/json");

        var response2 = await host.Client.SendAsync(request2);

        response2.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}
