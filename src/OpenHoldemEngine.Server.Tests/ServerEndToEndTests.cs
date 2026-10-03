using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenHoldemEngine.Server.Tests;

/// <summary>Drives the real <c>src/OpenHoldemEngine.Server</c> application over HTTP, on a database of its own for each provider.</summary>
[TestClass]
public sealed class ServerEndToEndTests
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

    /// <summary>
    /// Runs <paramref name="test"/> against a server host over a fresh database of <paramref name="providerName"/>;
    /// a SQLite file is deleted afterwards, a server database dies with its container.
    /// </summary>
    private static async Task RunAsync(string providerName, bool migrateOnStartup, Func<HttpClient, Task> test)
    {
        var connectionString = ServerDatabases.ConnectionStringFor(Enum.Parse<HoldemServerProvider>(providerName));
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                // Not Development, so appsettings.Development.json cannot hide a missing setting.
                builder.UseEnvironment("Production");
                builder.UseSetting("HoldemServer:Provider", providerName);
                builder.UseSetting("ConnectionStrings:Holdem", connectionString);
                builder.UseSetting("HoldemEngine:MigrateDatabaseOnStartup", migrateOnStartup ? "true" : "false");
            });
            using var client = factory.CreateClient();
            await test(client);
        }
        finally
        {
            if (providerName == nameof(HoldemServerProvider.Sqlite))
            {
                // Release the pooled connections, or Windows keeps the file locked.
                SqliteConnection.ClearAllPools();
                var dbFile = new SqliteConnectionStringBuilder(connectionString).DataSource;
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(dbFile)!, Path.GetFileName(dbFile) + "*"))
                {
                    File.Delete(file);
                }
            }
        }
    }

    [TestMethod]
    [DataRow("Sqlite")]
    [DataRow("PostgreSql")]
    [DataRow("SqlServer")]
    public Task Server_PlaysAGameFromCreationToFinish(string providerName) => RunAsync(providerName, true, async client =>
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
    [DataRow("Sqlite", "Microsoft.EntityFrameworkCore.Sqlite")]
    [DataRow("PostgreSql", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [DataRow("SqlServer", "Microsoft.EntityFrameworkCore.SqlServer")]
    public Task Server_Health_ReportsTheSchemaState(string providerName, string expectedEfProvider) => RunAsync(providerName, true, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        var data = body.GetProperty("entries").GetProperty("openholdemengine").GetProperty("data");
        data.GetProperty("state").GetString().Should().Be("UpToDate");
        data.GetProperty("providerName").GetString().Should().Be(expectedEfProvider);
    });

    [TestMethod]
    public Task Server_Health_WithoutMigration_IsUnhealthy() => RunAsync("Sqlite", false, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await ReadJsonAsync(response);
        body.GetProperty("entries").GetProperty("openholdemengine").GetProperty("data").GetProperty("state").GetString().Should().Be("MigrationRequired");
    });
}
