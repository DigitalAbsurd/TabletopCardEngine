# TabletopCardEngine.OpenTelemetry

OpenTelemetry instrumentation for `TabletopCardEngine`. Adds automatic tracing and metrics to the engine with a single method call.

```bash
dotnet add package TabletopCardEngine.OpenTelemetry
```

Wire up OpenTelemetry tracing and metrics with the instrumentation:

```csharp
using OpenTelemetry;
using TabletopCardEngine.OpenTelemetry;

var services = new ServiceCollection();
services
    .AddOpenTelemetry()
    .AddTabletopCardEngineInstrumentation()
    .WithTracing(tracing => tracing.AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddConsoleExporter());
```

The engine publishes activity spans for each action (`ApplyAsync`) with tags for game ID, hand number, and error codes. Metrics include action counts, durations, and schema checks. Logs are written to `ILogger<HoldemTable>` at Debug, Information, and Trace levels depending on the signal.

**Documentation:** [Observability](https://tabletopcardengine.readthedocs.io/en/stable/observability/) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
