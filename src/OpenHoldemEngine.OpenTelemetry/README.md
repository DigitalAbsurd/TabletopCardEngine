# OpenHoldemEngine.OpenTelemetry

OpenTelemetry instrumentation for `OpenHoldemEngine`. Adds automatic tracing and metrics to the engine with a single method call.

```bash
dotnet add package OpenHoldemEngine.OpenTelemetry --prerelease
```

Wire up OpenTelemetry tracing and metrics with the instrumentation:

```csharp
using OpenTelemetry;
using OpenHoldemEngine.OpenTelemetry;

var services = new ServiceCollection();
services
    .AddOpenTelemetry()
    .AddOpenHoldemEngineInstrumentation()
    .WithTracing(tracing => tracing.AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddConsoleExporter());
```

The engine publishes activity spans for each action (`ApplyHoldemActionAsync`) with tags for game ID, hand number, and error codes. Metrics include action counts, durations, and schema checks. Logs are written to `ILogger<HoldemEngine>` at Debug, Information, and Trace levels depending on the signal.

**Documentation:** [Observability](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/observability.md) · [Repository](https://github.com/miekhra/OpenHoldemEngine)
