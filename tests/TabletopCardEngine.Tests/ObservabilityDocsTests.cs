using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Reflection;
using TabletopCardEngine.Diagnostics;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Keeps <c>docs/observability.md</c> in step with the diagnostics code. Every log event,
/// span tag, and metric instrument defined in <c>Diagnostics/</c> and <c>TabletopCardEngineTelemetry</c>
/// must be documented. Event IDs and names are part of the public contract.
/// </summary>
[TestClass]
public sealed partial class ObservabilityDocsTests
{
    private const string ObservabilityFileName = "docs/observability.md";

    [TestMethod]
    public void ObservabilityDocs_DocumentsEveryLogEvent()
    {
        var doc = ReadObservabilityDoc();

        // Find the Logs section
        var logsStart = doc.IndexOf("\n## Logs", StringComparison.Ordinal);
        logsStart.Should().BeGreaterThan(0, "docs/observability.md must have a '## Logs' section");

        var logsEnd = doc.IndexOf("\n## ", logsStart + 1, StringComparison.Ordinal);
        var logsSection = logsEnd < 0 ? doc[logsStart..] : doc[logsStart..logsEnd];

        // Get all LoggerMessage methods from HoldemEngineLog
        var logMethods = typeof(HoldemEngineLog)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<LoggerMessageAttribute>() is not null)
            .ToList();

        logMethods.Should().NotBeEmpty();

        var problems = new List<string>();
        foreach (var method in logMethods)
        {
            var attr = method.GetCustomAttribute<LoggerMessageAttribute>()!;
            var eventId = attr.EventId;
            var eventName = method.Name;

            // Check that both EventId and method name appear in a row
            if (!logsSection.Contains(eventId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                problems.Add($"Event {eventId} ({eventName}) is not documented");
            }

            if (!logsSection.Contains(eventName, StringComparison.Ordinal))
            {
                problems.Add($"Method {eventName} (EventId {eventId}) is not documented");
            }
        }

        problems.Should().BeEmpty("every log event must be in the Logs table");
    }

    [TestMethod]
    public void ObservabilityDocs_DocumentsEveryActivityTag()
    {
        var doc = ReadObservabilityDoc();

        // Find the Traces section
        var tracesStart = doc.IndexOf("\n## Traces", StringComparison.Ordinal);
        tracesStart.Should().BeGreaterThan(0, "docs/observability.md must have a '## Traces' section");

        var tracesEnd = doc.IndexOf("\n## ", tracesStart + 1, StringComparison.Ordinal);
        var tracesSection = tracesEnd < 0 ? doc[tracesStart..] : doc[tracesStart..tracesEnd];

        // Get all tag name constants from HoldemEngineActivity
        var tagConstants = typeof(HoldemEngineActivity)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(f => f.Name.EndsWith("Tag", StringComparison.Ordinal) && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        tagConstants.Should().NotBeEmpty();

        var problems = new List<string>();
        foreach (var tagName in tagConstants)
        {
            if (!tracesSection.Contains(tagName, StringComparison.Ordinal))
            {
                problems.Add($"Tag '{tagName}' is not documented");
            }
        }

        problems.Should().BeEmpty("every activity tag must be in the Traces section");
    }

    [TestMethod]
    public void ObservabilityDocs_DocumentsEveryMetricInstrument()
    {
        var doc = ReadObservabilityDoc();

        // Find the Metrics section
        var metricsStart = doc.IndexOf("\n## Metrics", StringComparison.Ordinal);
        metricsStart.Should().BeGreaterThan(0, "docs/observability.md must have a '## Metrics' section");

        var metricsEnd = doc.IndexOf("\n## ", metricsStart + 1, StringComparison.Ordinal);
        var metricsSection = metricsEnd < 0 ? doc[metricsStart..] : doc[metricsStart..metricsEnd];

        // Collect instruments by creating an engine with a meter listener
        var instruments = CollectInstruments();

        var problems = new List<string>();
        foreach (var (name, unit) in instruments)
        {
            if (!metricsSection.Contains(name, StringComparison.Ordinal))
            {
                problems.Add($"Instrument '{name}' is not documented");
            }

            if (unit is not null && !metricsSection.Contains(unit, StringComparison.Ordinal))
            {
                problems.Add($"Unit '{unit}' for '{name}' is not documented");
            }
        }

        problems.Should().BeEmpty("every metric instrument must be in the Metrics table");
    }

    [TestMethod]
    public void ObservabilityDocs_ContainsActivitySourceName()
    {
        var doc = ReadObservabilityDoc();
        doc.Should().Contain(TabletopCardEngineTelemetry.ActivitySourceName,
            "ActivitySourceName must appear verbatim in docs/observability.md");
    }

    [TestMethod]
    public void ObservabilityDocs_ContainsMeterName()
    {
        var doc = ReadObservabilityDoc();
        doc.Should().Contain(TabletopCardEngineTelemetry.MeterName,
            "MeterName must appear verbatim in docs/observability.md");
    }

    [TestMethod]
    public void ObservabilityDocs_ContainsSpanName()
    {
        var doc = ReadObservabilityDoc();
        doc.Should().Contain(HoldemEngineActivity.Name,
            "Span name must appear verbatim in docs/observability.md");
    }

    private static string ReadObservabilityDoc()
    {
        var path = Path.Combine(RepositoryRoot.FullPath, ObservabilityFileName);
        File.Exists(path).Should().BeTrue("docs/observability.md must exist at {0}", path);
        return File.ReadAllText(path);
    }

    private static List<(string Name, string? Unit)> CollectInstruments()
    {
        var instruments = new List<(string Name, string? Unit)>();

        // Create a test container with AddMetrics so the engine creates a meter
        var services = new ServiceCollection();
        services.AddLogging();

        // Create a meter listener to capture instruments
        var listener = new MeterListener();
        listener.InstrumentPublished += (instrument, listener) =>
        {
            if (instrument.Meter.Name == TabletopCardEngineTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
                instruments.Add((instrument.Name, instrument.Unit));
            }
        };

        listener.Start();

        try
        {
            // Create the engine which will create the meter and instruments
            var meterFactory = new TestMeterFactory();
            var metrics = HoldemEngineMetrics.CreateOrGetFallback(meterFactory);

            // Force the listener to process published instruments by creating a recording
            Thread.Sleep(100);
        }
        finally
        {
            listener.Dispose();
        }

        return instruments;
    }

    /// <summary>A test IMeterFactory that collects created meters.</summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
        }
    }
}
