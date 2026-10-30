using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace TabletopCardEngine.Server.Tests;

/// <summary>
/// Builds the <c>sqlite</c>, <c>postgres</c> and <c>mssql</c> targets of the repository's
/// <c>Dockerfile</c> and drives each image over HTTP, next to a database of its own. The in-process
/// tests never see the build context, the user or the environment defaults of a target; these do.
/// </summary>
/// <remarks>
/// Each image runs with only the environment <c>docs/docker.md</c> tells a user to set, so the
/// provider and the start-up migration come from the image itself. Runs only under
/// <see cref="ServerDatabases.ContainersVariable"/>=1, because it needs Docker.
/// </remarks>
[TestClass]
public sealed class ContainerImageTests
{
    private const int HttpPort = 8080;

    // The images' non-root user, APP_UID of the mcr.microsoft.com/dotnet base images.
    private const string AppUid = "1654";

    private static readonly string[] _targets = ["sqlite", "postgres", "mssql"];

    // The previous stable release, pulled from Docker Hub by digest: a tag can be re-pushed, a digest
    // cannot. Raised together with the package validation baseline after each stable minor.
    private static readonly Dictionary<string, string> _previousStableImages = new()
    {
        ["sqlite"] = "digitalabsurd/tabletopcardengine:1.0.0-sqlite@sha256:adf9e76f50bef50aa17096e78b1a29270e2442f1fbcf4ef1cc47706353e0d1f5",
        ["postgres"] = "digitalabsurd/tabletopcardengine:1.0.0-postgres@sha256:681f828605b923ac5fc003f9bde647d9e67f8180a4e89ff4cf4fbdd594c3f39d",
        ["mssql"] = "digitalabsurd/tabletopcardengine:1.0.0-mssql@sha256:b3fa5f256bb5177531bf2e49e24a04a3f391982d4d0d25570a991c72d18c6e6f",
    };

    // Built one after another in one task: the targets share the builder stage, and three builds
    // started side by side from three tests would race on it.
    private static readonly Lazy<Task<IReadOnlyDictionary<string, IFutureDockerImage>>> _images = new(BuildImagesAsync);

    private static async Task<IReadOnlyDictionary<string, IFutureDockerImage>> BuildImagesAsync()
    {
        var images = new Dictionary<string, IFutureDockerImage>();
        foreach (var target in _targets)
        {
            var image = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
                .WithDockerfile("Dockerfile")
                .WithTarget(target)
                .WithName($"tabletopcardengine-test:{target}")
                .WithCleanUp(true)
                .Build();
            await image.CreateAsync();
            images.Add(target, image);
        }
        return images;
    }

    private static async Task<IFutureDockerImage> ImageAsync(string target)
    {
        if (!ServerDatabases.ContainersEnabled)
        {
            Assert.Inconclusive($"Set {ServerDatabases.ContainersVariable}=1 (needs Docker) to build and run the container images");
        }

        return (await _images.Value)[target];
    }

    private static HttpClient ClientFor(IContainer container)
        => new() { BaseAddress = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(HttpPort)}") };

    private static string ExpectedProviderName(string target) => target switch
    {
        "sqlite" => "Microsoft.EntityFrameworkCore.Sqlite",
        "postgres" => "Npgsql.EntityFrameworkCore.PostgreSQL",
        "mssql" => "Microsoft.EntityFrameworkCore.SqlServer",
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Not a target of the Dockerfile."),
    };

    /// <summary>
    /// Starts the database container aliased <c>db</c> on <paramref name="network"/> when the target needs
    /// one, and returns it with the environment that points a server container at it.
    /// </summary>
    private static async Task<(IContainer? Database, Dictionary<string, string> Environment)> StartDatabaseAsync(string target, INetwork network)
    {
        var environment = new Dictionary<string, string>();
        IContainer? database = null;
        switch (target)
        {
            case "sqlite":
                break;
            case "postgres":
                database = new PostgreSqlBuilder("postgres:16-alpine").WithNetwork(network).WithNetworkAliases("db").Build();
                await database.StartAsync();
                environment["ConnectionStrings__TabletopCardEngine"] = "Host=db;Database=holdem;Username=postgres;Password=postgres";
                break;
            case "mssql":
                var sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").WithNetwork(network).WithNetworkAliases("db").Build();
                database = sqlServer;
                await sqlServer.StartAsync();
                var password = new SqlConnectionStringBuilder(sqlServer.GetConnectionString()).Password;
                environment["ConnectionStrings__TabletopCardEngine"] = $"Server=db;Database=holdem;User Id=sa;Password={password};TrustServerCertificate=True";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Not a target of the Dockerfile.");
        }
        return (database, environment);
    }

    /// <summary>
    /// Starts a server container from <paramref name="image"/> on <paramref name="network"/> with exactly
    /// <paramref name="environment"/>, and waits until <c>/health</c> answers.
    /// </summary>
    private static async Task<IContainer> StartServerAsync(IImage image, INetwork network, IReadOnlyDictionary<string, string> environment, IVolume? dataVolume = null)
    {
        var builder = new ContainerBuilder(image)
            .WithNetwork(network)
            .WithPortBinding(HttpPort, true)
            .WithEnvironment(environment)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/health")));
        if (dataVolume is not null)
        {
            builder = builder.WithVolumeMount(dataVolume, "/data");
        }

        var server = builder.Build();
        await server.StartAsync();
        return server;
    }

    /// <summary>
    /// Runs the image of <paramref name="target"/> on a network of its own, next to a database
    /// container aliased <c>db</c> when the target needs one, waits until <c>/health</c> answers, and
    /// hands the running container to <paramref name="test"/>. Everything started is disposed afterwards.
    /// </summary>
    private static async Task RunAsync(string target, Func<IContainer, Task> test)
    {
        var image = await ImageAsync(target);

        await using var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        IContainer? database = null;
        try
        {
            Dictionary<string, string> environment;
            (database, environment) = await StartDatabaseAsync(target, network);

            await using var server = await StartServerAsync(image, network, environment);

            await test(server);
        }
        finally
        {
            if (database is not null)
            {
                await database.DisposeAsync();
            }
        }
    }

    private static async Task<JsonElement> HealthDataAsync(HttpClient client)
    {
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await GameScenario.ReadJsonAsync(response)).GetProperty("entries").GetProperty("tabletopcardengine-holdem").GetProperty("data");
    }
    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public Task Image_PlaysAGameOverHttp(string target) => RunAsync(target, async server =>
    {
        using var client = ClientFor(server);

        await GameScenario.PlayFromCreationToFinishAsync(client);
    });

    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public Task Image_Health_IsUpToDate(string target) => RunAsync(target, async server =>
    {
        using var client = ClientFor(server);

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await GameScenario.ReadJsonAsync(response)).GetProperty("entries").GetProperty("tabletopcardengine-holdem").GetProperty("data");
        data.GetProperty("state").GetString().Should().Be("UpToDate");
        data.GetProperty("providerName").GetString().Should().Be(ExpectedProviderName(target));
    });

    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public Task Image_OpenApi_ListsEveryEndpoint(string target) => RunAsync(target, async server =>
    {
        using var client = ClientFor(server);

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var operationIds = (await GameScenario.ReadJsonAsync(response)).GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(operation => operation.Value.ValueKind == JsonValueKind.Object && operation.Value.TryGetProperty("operationId", out _))
            .Select(operation => operation.Value.GetProperty("operationId").GetString())
            .Order()
            .ToList();
        operationIds.Should().Equal(
            "HoldemCreateGame",
            "HoldemFinishGame",
            "HoldemGetGame",
            "HoldemPlay",
            "HoldemStartGame",
            "HoldemStartHand");
    });

    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public Task Image_RunsAsNonRoot(string target) => RunAsync(target, async server =>
    {
        var result = await server.ExecAsync(["id", "-u"]);

        result.ExitCode.Should().Be(0, result.Stderr);
        result.Stdout.Trim().Should().Be(AppUid);
    });

    [TestMethod]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public async Task Image_ServerTargets_FailFastWithoutConnectionString(string target)
    {
        var image = await ImageAsync(target);
        await using var server = new ContainerBuilder(image).Build();
        await server.StartAsync();

        // The image has no HEALTHCHECK, so wait for the process to exit, but not for ever.
        // GetExitCodeAsync waits for the exit; a container still running would read as 0 below.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var exitCode = await server.GetExitCodeAsync(timeout.Token);

        exitCode.Should().NotBe(0);
        var (stdout, stderr) = await server.GetLogsAsync();
        (stdout + stderr).Should().Contain("ConnectionStrings__TabletopCardEngine");
    }

    [TestMethod]
    public Task Image_Sqlite_KeepsDataInTheVolume() => RunAsync("sqlite", async server =>
    {
        string gameUrl;
        string etag;
        using (var client = ClientFor(server))
        {
            var created = await client.PostAsync("/api/holdem/games", GameScenario.Json("{\"options\":{\"maxPlayersCount\":2}}"));
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            gameUrl = $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(created)).GetProperty("gameId").GetGuid()}";
            var before = await client.GetAsync(gameUrl);
            before.StatusCode.Should().Be(HttpStatusCode.OK);
            before.Headers.ETag.Should().NotBeNull();
            etag = before.Headers.ETag!.Tag;
        }

        await server.StopAsync();
        await server.StartAsync();

        // The mapped port changes with the restart, so the client is made anew.
        using (var client = ClientFor(server))
        {
            var after = await client.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag.Should().NotBeNull();
            after.Headers.ETag!.Tag.Should().Be(etag);
        }
    });

    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public async Task Image_UpgradesFromThePreviousStableRelease(string target)
    {
        var current = await ImageAsync(target);
        var previous = new DockerImage(_previousStableImages[target]);

        await using var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // A named volume is filled from the image's /data on first mount, so the old image must mount it first.
        await using var volume = new VolumeBuilder().Build();
        await volume.CreateAsync();

        IContainer? database = null;
        try
        {
            Dictionary<string, string> environment;
            (database, environment) = await StartDatabaseAsync(target, network);
            var dataVolume = target == "sqlite" ? volume : null;

            string gameUrl;
            string etag;
            JsonElement before;
            await using (var old = await StartServerAsync(previous, network, environment, dataVolume))
            {
                using var client = ClientFor(old);
                (await HealthDataAsync(client)).GetProperty("state").GetString().Should().Be("UpToDate");

                var created = await client.PostAsync("/api/holdem/games", GameScenario.Json(GameScenario.HeadsUpCreateBody));
                created.StatusCode.Should().Be(HttpStatusCode.Created);
                gameUrl = $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(created)).GetProperty("gameId").GetGuid()}";
                var started = await client.SendAsync(GameScenario.Post($"{gameUrl}/start", GameScenario.HeadsUpStartBody));
                started.StatusCode.Should().Be(HttpStatusCode.OK);
                await PlayOneActionAsync(client, gameUrl, await GameScenario.ReadJsonAsync(started));

                var read = await client.GetAsync(gameUrl);
                read.StatusCode.Should().Be(HttpStatusCode.OK);
                etag = read.Headers.ETag!.Tag;
                before = await GameScenario.ReadJsonAsync(read);
                before.GetProperty("options").TryGetProperty("tableFormat", out _).Should().BeFalse("1.0.0 has no table format");
            }

            await using var upgraded = await StartServerAsync(current, network, environment, dataVolume);
            using var upgradedClient = ClientFor(upgraded);
            (await HealthDataAsync(upgradedClient)).GetProperty("state").GetString().Should().Be("UpToDate");

            var after = await upgradedClient.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag!.Tag.Should().Be(etag, "the migration does not bump Game.Version");
            var afterBody = await GameScenario.ReadJsonAsync(after);
            afterBody.GetProperty("options").GetProperty("tableFormat").GetString().Should().Be("SixMax");
            WithoutTableFormat(afterBody).Should().Be(JsonNode.Parse(before.GetRawText())!.ToJsonString(), "the rest of the game survives the upgrade");

            var version = GameScenario.VersionOf(after);
            var played = await PlayOneActionAsync(upgradedClient, gameUrl, afterBody);
            GameScenario.VersionOf(played).Should().Be(version + 1);
        }
        finally
        {
            if (database is not null)
            {
                await database.DisposeAsync();
            }
        }
    }

    private static string WithoutTableFormat(JsonElement game)
    {
        var node = JsonNode.Parse(game.GetRawText())!;
        node["options"]!.AsObject().Remove("tableFormat");
        return node.ToJsonString();
    }

    private static async Task<HttpResponseMessage> PlayOneActionAsync(HttpClient client, string gameUrl, JsonElement game)
    {
        var offered = game.GetProperty("handData").GetProperty("evaluations").EnumerateArray()
            .Select(evaluation => evaluation.GetProperty("playerActionType").GetString()).ToList();
        var action = offered.Contains("Check") ? "Check" : "Call";
        var played = await client.SendAsync(GameScenario.Post($"{gameUrl}/actions", $"{{\"actionType\":\"{action}\"}}"));
        played.StatusCode.Should().Be(HttpStatusCode.OK);
        return played;
    }
}
