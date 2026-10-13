using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
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
            var environment = new Dictionary<string, string>();
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

            await using var server = new ContainerBuilder(image)
                .WithNetwork(network)
                .WithPortBinding(HttpPort, true)
                .WithEnvironment(environment)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/health")))
                .Build();
            await server.StartAsync();

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
}
