using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TabletopCardEngine.AspNetCore.Tests;

/// <summary>Drives the real <c>samples/AspNetCoreSample</c> application over HTTP, on a SQLite file of its own.</summary>
[TestClass]
public sealed class SampleEndToEndTests
{
    // A hand of two players needs a handful of actions; more than this means the action choice is wrong.
    private const int MaxRequestsPerHand = 20;

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static int VersionOf(HttpResponseMessage response) => int.Parse(response.Headers.ETag!.Tag.Trim('"'), System.Globalization.CultureInfo.InvariantCulture);

    private static HttpRequestMessage Post(string url, string? body, string? ifMatch = null)
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

    /// <summary>Runs <paramref name="test"/> against a sample host whose database is a fresh temporary file, deleted afterwards.</summary>
    private static async Task RunAsync(bool migrateOnStartup, Func<HttpClient, Task> test)
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"tce-sample-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:TabletopCardEngine", $"Data Source={dbFile}");
                builder.UseSetting("TabletopCardEngine:Holdem:MigrateDatabaseOnStartup", migrateOnStartup ? "true" : "false");
            });
            using var client = factory.CreateClient();
            await test(client);
        }
        finally
        {
            // Release the pooled connections, or Windows keeps the file locked.
            SqliteConnection.ClearAllPools();
            foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(dbFile) + "*"))
            {
                File.Delete(file);
            }
        }
    }

    [TestMethod]
    public Task Sample_PlaysAGameFromCreationToFinish() => RunAsync(true, async client =>
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
    });

    [TestMethod]
    public Task Sample_Health_ReportsTheSchemaState() => RunAsync(true, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("entries").GetProperty("tabletopcardengine-holdem").GetProperty("data").GetProperty("state").GetString().Should().Be("UpToDate");
    });

    [TestMethod]
    public Task Sample_Health_WithoutMigration_IsUnhealthy() => RunAsync(false, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await ReadJsonAsync(response);
        body.GetProperty("entries").GetProperty("tabletopcardengine-holdem").GetProperty("data").GetProperty("state").GetString().Should().Be("MigrationRequired");
    });

    [TestMethod]
    public Task Sample_LeaveAndRejoin_PlaysTwoHandsWithSeatChange() => RunAsync(true, async client =>
    {
        var created = await client.PostAsync("/api/holdem/games", Json("{\"options\":{\"maxPlayersCount\":6}}"));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var gameId = (await ReadJsonAsync(created)).GetProperty("gameId").GetGuid();
        var gameUrl = $"/api/holdem/games/{gameId}";

        var started = await client.SendAsync(Post($"{gameUrl}/start",
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100}]}"));
        started.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = VersionOf(started);

        // Play hand 1 to completion
        var game = await ReadJsonAsync(started);
        while (game.GetProperty("handData").GetProperty("evaluations").GetArrayLength() > 0)
        {
            var offered = game.GetProperty("handData").GetProperty("evaluations").EnumerateArray()
                .Select(evaluation => evaluation.GetProperty("playerActionType").GetString()).ToList();
            var action = offered.Contains("Check") ? "Check" : "Call";

            var played = await client.SendAsync(Post($"{gameUrl}/actions", $"{{\"actionType\":\"{action}\"}}"));
            played.StatusCode.Should().Be(HttpStatusCode.OK);
            version = VersionOf(played);
            game = await ReadJsonAsync(played);
        }

        // Leave seat 1
        var left = await client.SendAsync(Post($"{gameUrl}/seats/1/leave", null));
        left.StatusCode.Should().Be(HttpStatusCode.OK);
        game = await ReadJsonAsync(left);
        var seat1Left = game.GetProperty("handData").GetProperty("seats").EnumerateArray()
            .First(s => s.GetProperty("seatNo").GetInt32() == 1);
        seat1Left.GetProperty("hasLeft").GetBoolean().Should().BeTrue();

        // Start hand 2 with a rejoin at seat 1
        var handTwo = await client.SendAsync(Post($"{gameUrl}/hands",
            "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":50}]}"));
        handTwo.StatusCode.Should().Be(HttpStatusCode.OK);
        game = await ReadJsonAsync(handTwo);
        game.GetProperty("handData").GetProperty("no").GetInt32().Should().Be(2);
        var seatsAfterRejoin = game.GetProperty("handData").GetProperty("seats").EnumerateArray().ToList();
        seatsAfterRejoin.Should().HaveCount(2);
        var seat1Rejoined = seatsAfterRejoin.First(s => s.GetProperty("seatNo").GetInt32() == 1);
        seat1Rejoined.GetProperty("hasLeft").GetBoolean().Should().BeFalse();
        var seat2 = seatsAfterRejoin.First(s => s.GetProperty("seatNo").GetInt32() == 2);
        seat2.GetProperty("hasLeft").GetBoolean().Should().BeFalse();
    });
}
