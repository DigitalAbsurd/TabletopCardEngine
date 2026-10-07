using FluentAssertions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;
using TabletopCardEngine.Diagnostics;
using TabletopCardEngine.Dto;
using TabletopCardEngine.OpenTelemetry;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The <c>TabletopCardEngine.OpenTelemetry</c> package against the real OpenTelemetry SDK, and the
/// guard that keeps the SDK out of the core library. The engine's source and meter are process-wide
/// and the assembly runs tests in parallel, so the span test filters by its own game id and the
/// metrics test only asserts what must be there at least.
/// </summary>
[TestClass]
public sealed class OpenTelemetryPackageTests
{
    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    [TestMethod]
    public async Task Instrumentation_OneLine_EnablesTracesAndMetrics()
    {
        var gameId = Guid.NewGuid();
        var spans = new List<Activity>();
        var metrics = new List<Metric>();
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddTabletopCardEngineInstrumentation()
            .AddInMemoryExporter(spans)
            .Build();
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddTabletopCardEngineInstrumentation()
            .AddInMemoryExporter(metrics)
            .Build();

        using (tracerProvider)
        using (meterProvider)
        using (TestEngineHost.Setup(out var engine, out _))
        {
            await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
            await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        }

        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        spans
            .Where(activity => activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
            .Select(activity => activity.DisplayName)
            .Should().Equal(HoldemEngineActivity.Name, HoldemEngineActivity.Name);

        metrics.Select(metric => metric.Name).Should().Contain(["tabletopcardengine.actions", "tabletopcardengine.action.duration"]);
    }

    [TestMethod]
    public async Task Instrumentation_ExportsTheEngineSpan()
    {
        var gameId = Guid.NewGuid();
        var exported = new List<Activity>();
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddTabletopCardEngineInstrumentation()
            .AddInMemoryExporter(exported)
            .Build();

        using (TestEngineHost.Setup(out var engine, out _))
        {
            await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
            await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        }

        tracerProvider.Dispose();

        exported
            .Where(activity => activity.GetTagItem(HoldemEngineActivity.GameIdTag)?.ToString() == gameId.ToString("D"))
            .Select(activity => activity.DisplayName)
            .Should().Equal(HoldemEngineActivity.Name, HoldemEngineActivity.Name);
    }

    [TestMethod]
    public async Task Instrumentation_ExportsTheEngineMetrics()
    {
        var exported = new List<Metric>();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddTabletopCardEngineInstrumentation()
            .AddInMemoryExporter(exported)
            .Build();

        // No AddMetrics() in this container: the engine falls back to the shared meter, which is
        // the one an SDK subscribed by name picks up.
        using (TestEngineHost.Setup(out var engine, out _))
        {
            var gameId = Guid.NewGuid();
            await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
            await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
            await ApplyAsync(engine, new HoldemActionGetGame(gameId));
        }

        meterProvider.ForceFlush().Should().BeTrue();

        exported.Select(metric => metric.Name).Should().Contain(["tabletopcardengine.actions", "tabletopcardengine.action.duration"]);
    }

    [TestMethod]
    public void Core_DoesNotDependOnOpenTelemetry()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the library must be restored before the tests run");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));

        assets.RootElement.GetProperty("libraries").EnumerateObject()
            .Select(library => library.Name)
            .Where(name => name.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty("the core may depend on System.Diagnostics and the logging abstractions only, never on OpenTelemetry");

        var direct = new List<string>();
        foreach (var framework in assets.RootElement.GetProperty("project").GetProperty("frameworks").EnumerateObject())
        {
            if (!framework.Value.TryGetProperty("dependencies", out var dependencies))
            {
                continue;
            }

            direct.AddRange(dependencies.EnumerateObject()
                .Where(dependency => !(dependency.Value.TryGetProperty("autoReferenced", out var flag) && flag.GetBoolean()))
                .Where(dependency => dependency.Name != "MinVer")
                .Select(dependency => dependency.Name));
        }

        // Microsoft.Extensions.Diagnostics.Abstractions is where IMeterFactory lives: the metrics
        // (#185) resolve their Meter through it when the host registers one.
        // Microsoft.EntityFrameworkCore.Relational holds MigrateAsync and GetPendingMigrationsAsync
        // for IHoldemSchemaManager (#201), and references no database driver.
        // MinVer is excluded because it is PrivateAssets="all" and is build-time only (fact 30).
        direct.Should().BeEquivalentTo(
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.Extensions.Diagnostics.Abstractions",
            "Microsoft.Extensions.Logging.Abstractions");
    }

    private static async Task ApplyAsync(HoldemEngine engine, HoldemAction action)
    {
        var result = await engine.ApplyHoldemActionAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
    }
}
