using FluentAssertions;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenHoldemEngine.Server.Tests;

/// <summary>
/// The game both the in-process host and the container images are driven through, so that the two
/// gates check the same thing, and the HTTP helpers it is written with.
/// </summary>
internal static class GameScenario
{
    // A hand of two players needs a handful of actions; more than this means the action choice is wrong.
    private const int MaxRequestsPerHand = 20;

    internal static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    internal static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    internal static int VersionOf(HttpResponseMessage response) => int.Parse(response.Headers.ETag!.Tag.Trim('"'), CultureInfo.InvariantCulture);

    internal static HttpRequestMessage Post(string url, string? body, string? ifMatch = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
        {
            request.Content = Json(body);
        }
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }
        return request;
    }

    /// <summary>
    /// Plays a heads-up game from creation to finish: a hand of checks and calls, a stale
    /// <c>If-Match</c> that must be rejected, a second hand that ends in a fold, and the finish.
    /// </summary>
    internal static async Task PlayFromCreationToFinishAsync(HttpClient client)
    {
        var created = await client.PostAsync("/api/holdem/games", Json("{\"options\":{\"maxPlayersCount\":2}}"));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var gameId = (await ReadJsonAsync(created)).GetProperty("gameId").GetGuid();
        var gameUrl = $"/api/holdem/games/{gameId}";

        var started = await client.SendAsync(Post($"{gameUrl}/start",
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100}]}"));
        started.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = VersionOf(started);
        var game = await ReadJsonAsync(started);

        // Hand 1: check when it is offered, otherwise call, until nobody is to act.
        var requests = 0;
        var versionBeforeLastAction = version;
        while (game.GetProperty("handData").GetProperty("evaluations").GetArrayLength() > 0)
        {
            (++requests).Should().BeLessOrEqualTo(MaxRequestsPerHand, "a heads-up hand of checks and calls is short");
            var offered = game.GetProperty("handData").GetProperty("evaluations").EnumerateArray()
                .Select(evaluation => evaluation.GetProperty("playerActionType").GetString()).ToList();
            var action = offered.Contains("Check") ? "Check" : "Call";

            versionBeforeLastAction = version;
            var played = await client.SendAsync(Post($"{gameUrl}/actions", $"{{\"actionType\":\"{action}\"}}"));
            played.StatusCode.Should().Be(HttpStatusCode.OK);
            VersionOf(played).Should().Be(version + 1);
            version = VersionOf(played);
            game = await ReadJsonAsync(played);
        }

        // A stale If-Match is rejected, and the game is left as it was.
        var stale = await client.SendAsync(Post($"{gameUrl}/actions", "{\"actionType\":\"Check\"}", $"\"{versionBeforeLastAction}\""));
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        (await ReadJsonAsync(stale)).GetProperty("code").GetString().Should().Be("Conflict");
        var reloaded = await client.GetAsync(gameUrl);
        VersionOf(reloaded).Should().Be(version);

        // Hand 2: the seat to act folds.
        var nextHand = await client.SendAsync(Post($"{gameUrl}/hands", null));
        nextHand.StatusCode.Should().Be(HttpStatusCode.OK);
        game = await ReadJsonAsync(nextHand);
        game.GetProperty("handData").GetProperty("no").GetInt32().Should().Be(2);
        version = VersionOf(nextHand);

        var folded = await client.SendAsync(Post($"{gameUrl}/actions", "{\"actionType\":\"Fold\"}"));
        folded.StatusCode.Should().Be(HttpStatusCode.OK);
        version = VersionOf(folded);

        var finished = await client.SendAsync(Post($"{gameUrl}/finish", null));
        finished.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(finished)).GetProperty("isFinished").GetBoolean().Should().BeTrue();
        version = VersionOf(finished);

        var final = await client.GetAsync(gameUrl);
        final.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(final)).GetProperty("isFinished").GetBoolean().Should().BeTrue();
        VersionOf(final).Should().Be(version);
    }
}
