using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Text.Json;

namespace TabletopCardEngine.Server.Tests;

/// <summary>Validates the OpenAPI document served at /openapi/v1.json.</summary>
[TestClass]
public sealed class OpenApiDocumentTests
{
    private static readonly string[] _expectedStatuses = ["200", "400", "404", "409", "412", "422"];

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Runs <paramref name="test"/> against a server host over a fresh SQLite database;
    /// the database file is deleted afterwards.
    /// </summary>
    private static async Task RunAsync(Func<HttpClient, Task> test)
    {
        var connectionString = ServerDatabases.ConnectionStringFor(HoldemServerProvider.Sqlite);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("TabletopCardEngineServer:Provider", "Sqlite");
                builder.UseSetting("ConnectionStrings:TabletopCardEngine", connectionString);
                builder.UseSetting("TabletopCardEngine:Holdem:MigrateDatabaseOnStartup", "true");
            });
            using var client = factory.CreateClient();
            await test(client);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            var dbFile = new SqliteConnectionStringBuilder(connectionString).DataSource;
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(dbFile)!, Path.GetFileName(dbFile) + "*"))
            {
                File.Delete(file);
            }
        }
    }

    [TestMethod]
    public Task OpenApi_Document_IsServed() => RunAsync(async client =>
    {
        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var document = await ReadJsonAsync(response);
        document.GetProperty("openapi").GetString()!.Should().StartWith("3.");
    });

    [TestMethod]
    public Task OpenApi_Document_ListsEveryEndpoint() => RunAsync(async client =>
    {
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await ReadJsonAsync(response);
        var paths = document.GetProperty("paths");

        var pathKeys = paths.EnumerateObject().Select(p => p.Name).Order().ToList();
        pathKeys.Should().Equal(
            "/api/holdem/games",
            "/api/holdem/games/{gameId}",
            "/api/holdem/games/{gameId}/actions",
            "/api/holdem/games/{gameId}/finish",
            "/api/holdem/games/{gameId}/hands",
            "/api/holdem/games/{gameId}/start");

        var operationIds = new HashSet<string>();
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.TryGetProperty("operationId", out var operationId))
                {
                    operationIds.Add(operationId.GetString()!);
                }
            }
        }

        operationIds.Order().Should().Equal(
            "HoldemCreateGame",
            "HoldemFinishGame",
            "HoldemGetGame",
            "HoldemPlay",
            "HoldemStartGame",
            "HoldemStartHand");
    });

    [TestMethod]
    public Task OpenApi_Document_TagsTheEngine() => RunAsync(async client =>
    {
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await ReadJsonAsync(response);
        var paths = document.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.TryGetProperty("tags", out var tags))
                {
                    var tagList = tags.EnumerateArray().Select(t => t.GetString()).ToList();
                    tagList.Should().Contain("Holdem");
                }
            }
        }
    });

    [TestMethod]
    public Task OpenApi_Document_DescribesProblems() => RunAsync(async client =>
    {
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await ReadJsonAsync(response);
        var paths = document.GetProperty("paths");
        var playPath = paths.GetProperty("/api/holdem/games/{gameId}/actions");
        var postOperation = playPath.GetProperty("post");

        postOperation.TryGetProperty("operationId", out var operationId).Should().BeTrue();
        operationId.GetString().Should().Be("HoldemPlay");

        var responses = postOperation.GetProperty("responses");
        var actualStatuses = responses.EnumerateObject().Select(r => r.Name).Order().ToArray();

        actualStatuses.Should().Equal(_expectedStatuses.Order().ToArray());
    });
}
