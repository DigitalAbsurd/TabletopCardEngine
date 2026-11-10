using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using System.Globalization;
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

    // The previous stable release (1.2.1), pulled from Docker Hub by digest: a tag can be re-pushed, a digest
    // cannot. Raised together with the package validation baseline after each stable minor.
    private static readonly Dictionary<string, string> _previousStableImages = new()
    {
        ["sqlite"] = "digitalabsurd/tabletopcardengine:1.2.1-sqlite@sha256:460f8d5bd2a1eda05b1d42e23fba413bee6e988ace37c29a42babef7d389e785",
        ["postgres"] = "digitalabsurd/tabletopcardengine:1.2.1-postgres@sha256:96415c539f5f09f13361892f7d70329c805abde1595466c2ba9e11159a10e144",
        ["mssql"] = "digitalabsurd/tabletopcardengine:1.2.1-mssql@sha256:77bb14fc685b7a80cef70a2f671dd5537c9ec4acac28b970f24ce46bfcc556e5",
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
            "HoldemKickSeat",
            "HoldemLeaveSeat",
            "HoldemPlay",
            "HoldemStartGame",
            "HoldemStartHand",
            "HoldemTopUpSeat");
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
                before.GetProperty("options").TryGetProperty("raiseCap", out _).Should().BeFalse("1.2.1 has no raise cap option");
            }

            await using var upgraded = await StartServerAsync(current, network, environment, dataVolume);
            using var upgradedClient = ClientFor(upgraded);
            (await HealthDataAsync(upgradedClient)).GetProperty("state").GetString().Should().Be("UpToDate");

            var after = await upgradedClient.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag!.Tag.Should().Be(etag, "the migration does not bump Game.Version");
            var afterBody = await GameScenario.ReadJsonAsync(after);
            afterBody.GetProperty("options").GetProperty("tableFormat").GetString().Should().Be("SixMax");
            afterBody.GetProperty("options").GetProperty("allowTopUp").GetBoolean().Should().BeFalse();
            RaiseOf(before).GetProperty("minValue").GetDecimal().Should().Be(3, "1.2.1 offers the old minimum raise (#649)");
            RaiseOf(afterBody).GetProperty("minValue").GetDecimal().Should().Be(4, "1.3.0 offers a raise of at least two big blinds");
            WithoutFieldsChangedIn13(afterBody).Should().Be(WithoutFieldsChangedIn13(before), "the rest of the game survives the upgrade");

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

    [TestMethod]
    [DataRow("sqlite")]
    [DataRow("postgres")]
    [DataRow("mssql")]
    public async Task Image_UpgradedGameTakesAndLosesPlayers(string target)
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
            await using (var old = await StartServerAsync(previous, network, environment, dataVolume))
            {
                using var client = ClientFor(old);
                (await HealthDataAsync(client)).GetProperty("state").GetString().Should().Be("UpToDate");

                var created = await client.PostAsync("/api/holdem/games", GameScenario.Json("{\"options\":{\"maxPlayersCount\":3}}"));
                created.StatusCode.Should().Be(HttpStatusCode.Created);
                gameUrl = $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(created)).GetProperty("gameId").GetGuid()}";
                var started = await client.SendAsync(GameScenario.Post($"{gameUrl}/start", GameScenario.HeadsUpStartBody));
                started.StatusCode.Should().Be(HttpStatusCode.OK);
                await FoldToTheEndAsync(client, gameUrl, started);

                var read = await client.GetAsync(gameUrl);
                read.StatusCode.Should().Be(HttpStatusCode.OK);
                etag = read.Headers.ETag!.Tag;
            }

            await using var upgraded = await StartServerAsync(current, network, environment, dataVolume);
            using var upgradedClient = ClientFor(upgraded);
            (await HealthDataAsync(upgradedClient)).GetProperty("state").GetString().Should().Be("UpToDate");

            var after = await upgradedClient.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag!.Tag.Should().Be(etag, "the migration does not bump Game.Version");
            (await GameScenario.ReadJsonAsync(after)).GetProperty("options").GetProperty("tableFormat").GetString().Should().Be("SixMax");
            var version = GameScenario.VersionOf(after);

            // Seat 3 sits down between hands.
            var join = "{\"joinSeats\":[{\"name\":\"c\",\"seatNo\":3,\"buyInAmount\":100}]}";
            var hand = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/hands", join));
            hand.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(hand).Should().Be(version + 1);
            version = GameScenario.VersionOf(hand);
            (await SeatNumbersAsync(hand)).Should().Equal(1, 2, 3);
            version = await FoldToTheEndAsync(upgradedClient, gameUrl, hand);

            // Seat 1 stands up; the next hand is dealt to seats 2 and 3 only.
            version = await LeaveAsync(upgradedClient, gameUrl, 1, version);
            hand = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/hands", null));
            hand.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(hand).Should().Be(version + 1);
            (await SeatNumbersAsync(hand)).Should().Equal(2, 3);
            version = await FoldToTheEndAsync(upgradedClient, gameUrl, hand);

            // Seat 2 stands up too, which leaves one player: no hand can be dealt.
            version = await LeaveAsync(upgradedClient, gameUrl, 2, version);
            var refused = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/hands", null));
            refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await GameScenario.ReadJsonAsync(refused)).GetProperty("code").GetString().Should().Be("NotEnoughPlayers");
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
    public async Task Image_UpgradedGameTopsUpKicksAndRaisesBlinds(string target)
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
            await using (var old = await StartServerAsync(previous, network, environment, dataVolume))
            {
                using var client = ClientFor(old);
                (await HealthDataAsync(client)).GetProperty("state").GetString().Should().Be("UpToDate");

                var created = await client.PostAsync("/api/holdem/games", GameScenario.Json("{\"options\":{\"maxPlayersCount\":3}}"));
                created.StatusCode.Should().Be(HttpStatusCode.Created);
                gameUrl = $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(created)).GetProperty("gameId").GetGuid()}";
                var started = await client.SendAsync(GameScenario.Post($"{gameUrl}/start", GameScenario.HeadsUpStartBody));
                started.StatusCode.Should().Be(HttpStatusCode.OK);
                await FoldToTheEndAsync(client, gameUrl, started);

                var read = await client.GetAsync(gameUrl);
                read.StatusCode.Should().Be(HttpStatusCode.OK);
                etag = read.Headers.ETag!.Tag;
            }

            // Only the server container goes: the database container and the volume keep the game.
            await using var upgraded = await StartServerAsync(current, network, environment, dataVolume);
            using var upgradedClient = ClientFor(upgraded);
            (await HealthDataAsync(upgradedClient)).GetProperty("state").GetString().Should().Be("UpToDate");

            var after = await upgradedClient.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag!.Tag.Should().Be(etag, "the migrations do not bump Game.Version");
            var body = await GameScenario.ReadJsonAsync(after);
            var options = body.GetProperty("options");
            options.GetProperty("allowTopUp").GetBoolean().Should().BeFalse();
            options.GetProperty("allowRebuy").GetBoolean().Should().BeFalse();
            options.GetProperty("blindSchedule").ValueKind.Should().Be(JsonValueKind.Null);
            var hand = body.GetProperty("handData");
            hand.GetProperty("smallBlind").GetDecimal().Should().Be(options.GetProperty("initialSmallBlind").GetDecimal());
            hand.GetProperty("blindLevel").GetInt32().Should().Be(0);
            var version = GameScenario.VersionOf(after);

            // The old game's options are off, and fixed.
            var refusedTopUp = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/seats/1/top-up", "{\"amount\":10}"));
            refusedTopUp.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await GameScenario.ReadJsonAsync(refusedTopUp)).GetProperty("code").GetString().Should().Be("TopUpNotAllowed");

            version = await KickAsync(upgradedClient, gameUrl, 2, version);
            var newcomer = "{\"joinSeats\":[{\"name\":\"c\",\"seatNo\":2,\"buyInAmount\":100}]}";
            var next = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/hands", newcomer));
            next.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(next).Should().Be(version + 1);
            (await SeatNumbersAsync(next)).Should().Equal(1, 2);
            await FoldToTheEndAsync(upgradedClient, gameUrl, next);

            // A new game on the upgraded server plays the 1.2 features.
            var newOptions = "{\"options\":{\"maxPlayersCount\":3,\"allowTopUp\":true,\"allowRebuy\":true,\"blindSchedule\":{\"increaseEveryHands\":1}}}";
            var createdNew = await upgradedClient.PostAsync("/api/holdem/games", GameScenario.Json(newOptions));
            createdNew.StatusCode.Should().Be(HttpStatusCode.Created);
            var newUrl = $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(createdNew)).GetProperty("gameId").GetGuid()}";
            var startedNew = await upgradedClient.SendAsync(GameScenario.Post($"{newUrl}/start", GameScenario.HeadsUpStartBody));
            startedNew.StatusCode.Should().Be(HttpStatusCode.OK);
            var initialSmallBlind = (await GameScenario.ReadJsonAsync(startedNew)).GetProperty("options").GetProperty("initialSmallBlind").GetDecimal();
            version = await FoldToTheEndAsync(upgradedClient, newUrl, startedNew);

            var ended = await GameScenario.ReadJsonAsync(await upgradedClient.GetAsync(newUrl));
            var seat1Stack = SeatOf(ended, 1).GetProperty("stack").GetDecimal();

            var toppedUp = await upgradedClient.SendAsync(GameScenario.Post($"{newUrl}/seats/1/top-up", "{\"amount\":10}"));
            toppedUp.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(toppedUp).Should().Be(version + 1);
            version = GameScenario.VersionOf(toppedUp);
            SeatOf(await GameScenario.ReadJsonAsync(toppedUp), 1).GetProperty("pendingBuyIn").GetDecimal().Should().Be(10);

            var dealt = await upgradedClient.SendAsync(GameScenario.Post($"{newUrl}/hands", null));
            dealt.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(dealt).Should().Be(version + 1);
            version = GameScenario.VersionOf(dealt);
            var dealtBody = await GameScenario.ReadJsonAsync(dealt);
            SeatOf(dealtBody, 1).GetProperty("startHandStack").GetDecimal().Should().Be(seat1Stack + 10);
            var dealtHand = dealtBody.GetProperty("handData");
            dealtHand.GetProperty("smallBlind").GetDecimal().Should().Be(initialSmallBlind * 2);
            dealtHand.GetProperty("blindLevel").GetInt32().Should().Be(1);

            // The seat not on the clock is kicked; heads-up, that ends the hand.
            var offClock = dealtHand.GetProperty("nextPlayerSeatNo").GetInt32() == 1 ? 2 : 1;
            var kicked = await upgradedClient.SendAsync(GameScenario.Post($"{newUrl}/seats/{offClock}/kick", null));
            kicked.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(kicked).Should().Be(version + 1);
            var kickedHand = (await GameScenario.ReadJsonAsync(kicked)).GetProperty("handData");
            kickedHand.GetProperty("evaluations").GetArrayLength().Should().Be(0, "the kick left one seat in the pot");
            kickedHand.GetProperty("seats").EnumerateArray()
                .Sum(seat => seat.GetProperty("handResult").GetProperty("net").GetDecimal()).Should().Be(0);
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
    public async Task Image_UpgradedServerPlaysPotLimitAndFixedLimit(string target)
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
            await using (var old = await StartServerAsync(previous, network, environment, dataVolume))
            {
                using var client = ClientFor(old);
                (await HealthDataAsync(client)).GetProperty("state").GetString().Should().Be("UpToDate");

                gameUrl = await CreateAsync(client, "{\"options\":{\"maxPlayersCount\":3}}");
                var started = await client.SendAsync(GameScenario.Post($"{gameUrl}/start", GameScenario.HeadsUpStartBody));
                started.StatusCode.Should().Be(HttpStatusCode.OK);
                await FoldToTheEndAsync(client, gameUrl, started);

                var read = await client.GetAsync(gameUrl);
                read.StatusCode.Should().Be(HttpStatusCode.OK);
                etag = read.Headers.ETag!.Tag;
            }

            // Only the server container goes: the database container and the volume keep the game.
            await using var upgraded = await StartServerAsync(current, network, environment, dataVolume);
            using var upgradedClient = ClientFor(upgraded);
            (await HealthDataAsync(upgradedClient)).GetProperty("state").GetString().Should().Be("UpToDate");

            var after = await upgradedClient.GetAsync(gameUrl);
            after.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Headers.ETag!.Tag.Should().Be(etag, "the migration does not bump Game.Version");
            var options = (await GameScenario.ReadJsonAsync(after)).GetProperty("options");
            options.GetProperty("bettingStructure").GetString().Should().Be("NoLimit");
            options.GetProperty("raiseCap").ValueKind.Should().Be(JsonValueKind.Null);
            var bigBlind = options.GetProperty("initialSmallBlind").GetDecimal() * 2;
            var version = GameScenario.VersionOf(after);

            // The old game still plays No-Limit: the minimum raise is two big blinds.
            var hand = await upgradedClient.SendAsync(GameScenario.Post($"{gameUrl}/hands", null));
            hand.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(hand).Should().Be(version + 1);
            RaiseOf(await GameScenario.ReadJsonAsync(hand)).GetProperty("minValue").GetDecimal().Should().Be(2 * bigBlind);

            // A new Pot-Limit game: the first player may raise to two big blinds at least, and to the pot at most.
            var potUrl = await CreateAsync(upgradedClient, "{\"options\":{\"maxPlayersCount\":3,\"bettingStructure\":\"PotLimit\"}}");
            var potStart = await upgradedClient.SendAsync(GameScenario.Post($"{potUrl}/start", ThreeSeatsStartBody));
            potStart.StatusCode.Should().Be(HttpStatusCode.OK);
            version = GameScenario.VersionOf(potStart);
            var potRaise = RaiseOf(await GameScenario.ReadJsonAsync(potStart));
            potRaise.GetProperty("minValue").GetDecimal().Should().Be(2 * bigBlind);
            potRaise.GetProperty("maxValue").GetDecimal().Should().Be(3.5m * bigBlind);

            var raised = await upgradedClient.SendAsync(GameScenario.Post($"{potUrl}/actions", RaiseBody(3.5m * bigBlind)));
            raised.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(raised).Should().Be(version + 1);
            var overTheLimit = RaiseOf(await GameScenario.ReadJsonAsync(raised)).GetProperty("maxValue").GetDecimal() + 1;
            var refused = await upgradedClient.SendAsync(GameScenario.Post($"{potUrl}/actions", RaiseBody(overTheLimit)));
            refused.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await GameScenario.ReadJsonAsync(refused)).GetProperty("code").GetString().Should().Be("IllegalAction");

            // A new Fixed-Limit game capped at two bets: the big blind and one raise.
            var limitUrl = await CreateAsync(upgradedClient, "{\"options\":{\"maxPlayersCount\":2,\"bettingStructure\":\"FixedLimit\",\"raiseCap\":2}}");
            var limitStart = await upgradedClient.SendAsync(GameScenario.Post($"{limitUrl}/start", GameScenario.HeadsUpStartBody));
            limitStart.StatusCode.Should().Be(HttpStatusCode.OK);
            version = GameScenario.VersionOf(limitStart);
            var limitRaise = RaiseOf(await GameScenario.ReadJsonAsync(limitStart));
            limitRaise.GetProperty("minValue").GetDecimal().Should().Be(2 * bigBlind);
            limitRaise.GetProperty("maxValue").GetDecimal().Should().Be(2 * bigBlind);

            var limitRaised = await upgradedClient.SendAsync(GameScenario.Post($"{limitUrl}/actions", RaiseBody(2 * bigBlind)));
            limitRaised.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(limitRaised).Should().Be(version + 1);
            version = GameScenario.VersionOf(limitRaised);
            var game = await GameScenario.ReadJsonAsync(limitRaised);
            OfferedActions(game).Should().NotContain("Raise", "the cap of two bets is reached");

            var requests = 0;
            while (game.GetProperty("handData").GetProperty("evaluations").GetArrayLength() > 0)
            {
                (++requests).Should().BeLessOrEqualTo(20, "a capped heads-up hand of checks and calls is short");
                var action = OfferedActions(game).Contains("Check") ? "Check" : "Call";
                var played = await upgradedClient.SendAsync(GameScenario.Post($"{limitUrl}/actions", $"{{\"actionType\":\"{action}\"}}"));
                played.StatusCode.Should().Be(HttpStatusCode.OK);
                GameScenario.VersionOf(played).Should().Be(version + 1);
                version = GameScenario.VersionOf(played);
                game = await GameScenario.ReadJsonAsync(played);
            }
            game.GetProperty("handData").GetProperty("seats").EnumerateArray()
                .Sum(seat => seat.GetProperty("handResult").GetProperty("net").GetDecimal()).Should().Be(0);
        }
        finally
        {
            if (database is not null)
            {
                await database.DisposeAsync();
            }
        }
    }

    private const string ThreeSeatsStartBody = "{\"joinSeats\":[{\"name\":\"a\",\"seatNo\":1,\"buyInAmount\":100},{\"name\":\"b\",\"seatNo\":2,\"buyInAmount\":100},{\"name\":\"c\",\"seatNo\":3,\"buyInAmount\":100}]}";

    private static string RaiseBody(decimal size) => $"{{\"actionType\":\"Raise\",\"size\":{size.ToString(CultureInfo.InvariantCulture)}}}";

    private static async Task<string> CreateAsync(HttpClient client, string body)
    {
        var created = await client.PostAsync("/api/holdem/games", GameScenario.Json(body));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        return $"/api/holdem/games/{(await GameScenario.ReadJsonAsync(created)).GetProperty("gameId").GetGuid()}";
    }

    private static List<string?> OfferedActions(JsonElement game)
        => game.GetProperty("handData").GetProperty("evaluations").EnumerateArray()
            .Select(evaluation => evaluation.GetProperty("playerActionType").GetString()).ToList();

    private static JsonElement RaiseOf(JsonElement game)
        => game.GetProperty("handData").GetProperty("evaluations").EnumerateArray()
            .Single(evaluation => evaluation.GetProperty("playerActionType").GetString() == "Raise");

    private static JsonElement SeatOf(JsonElement game, int seatNo)
        => game.GetProperty("handData").GetProperty("seats").EnumerateArray().Single(seat => seat.GetProperty("seatNo").GetInt32() == seatNo);

    private static async Task<List<int>> SeatNumbersAsync(HttpResponseMessage response)
        => (await GameScenario.ReadJsonAsync(response)).GetProperty("handData").GetProperty("seats").EnumerateArray()
            .Select(seat => seat.GetProperty("seatNo").GetInt32()).ToList();

    /// <summary>Folds the seat on the clock, as the response names it, until nobody is to act. Returns the version.</summary>
    private static async Task<int> FoldToTheEndAsync(HttpClient client, string gameUrl, HttpResponseMessage response)
    {
        var version = GameScenario.VersionOf(response);
        var game = await GameScenario.ReadJsonAsync(response);
        var requests = 0;
        while (game.GetProperty("handData").GetProperty("evaluations").GetArrayLength() > 0)
        {
            (++requests).Should().BeLessOrEqualTo(10, "folding ends a hand of at most three seats in two requests");
            var folded = await client.SendAsync(GameScenario.Post($"{gameUrl}/actions", "{\"actionType\":\"Fold\"}"));
            folded.StatusCode.Should().Be(HttpStatusCode.OK);
            GameScenario.VersionOf(folded).Should().Be(version + 1);
            version = GameScenario.VersionOf(folded);
            game = await GameScenario.ReadJsonAsync(folded);
        }
        return version;
    }

    private static async Task<int> LeaveAsync(HttpClient client, string gameUrl, int seatNo, int version)
    {
        var left = await client.SendAsync(GameScenario.Post($"{gameUrl}/seats/{seatNo}/leave", null));
        left.StatusCode.Should().Be(HttpStatusCode.OK);
        GameScenario.VersionOf(left).Should().Be(version + 1);
        return GameScenario.VersionOf(left);
    }

    /// <summary>
    /// The game without what 1.3 adds or changes: the <c>raiseCap</c> option, and the minimum of the Raise evaluation,
    /// which 1.3 raises to two big blinds (#649). What is left must be equal on both sides of the upgrade.
    /// </summary>
    private static string WithoutFieldsChangedIn13(JsonElement game)
    {
        var node = JsonNode.Parse(game.GetRawText())!;
        node["options"]!.AsObject().Remove("raiseCap");
        foreach (var evaluation in node["handData"]!["evaluations"]!.AsArray())
        {
            if (evaluation!["playerActionType"]!.GetValue<string>() == "Raise")
            {
                evaluation.AsObject().Remove("minValue");
            }
        }
        return node.ToJsonString();
    }

    private static async Task<int> KickAsync(HttpClient client, string gameUrl, int seatNo, int version)
    {
        var kicked = await client.SendAsync(GameScenario.Post($"{gameUrl}/seats/{seatNo}/kick", null));
        kicked.StatusCode.Should().Be(HttpStatusCode.OK);
        GameScenario.VersionOf(kicked).Should().Be(version + 1);
        return GameScenario.VersionOf(kicked);
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
