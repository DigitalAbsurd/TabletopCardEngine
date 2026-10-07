using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace TabletopCardEngine.OpenTelemetry;

/// <summary>
/// Turns the engine's traces and metrics on in an OpenTelemetry host by name, so the host does not
/// have to know <see cref="HoldemEngineTelemetry.ActivitySourceName"/> and
/// <see cref="HoldemEngineTelemetry.MeterName"/>. The engine's logs, traces and metrics are described
/// in <c>docs/observability.md</c>.
/// <para>
/// Logging needs no extension here: the engine logs through <c>ILogger</c> under categories that
/// start with <c>TabletopCardEngine</c>, so a host enables that category in its logging configuration
/// and, if it wants OTLP logs, adds the OpenTelemetry SDK's <c>AddOpenTelemetry()</c> logging
/// provider.
/// </para>
/// </summary>
public static class TabletopCardEngineInstrumentationExtensions
{
    /// <summary>
    /// Subscribes the tracer provider to the engine's <c>ActivitySource</c>, which opens one
    /// <c>TabletopCardEngine.ApplyHoldemAction</c> span per call to
    /// <see cref="HoldemEngine.ApplyHoldemActionAsync"/>. The engine's telemetry is described in
    /// <c>docs/observability.md</c>.
    /// </summary>
    /// <param name="builder">The tracer provider builder of the host.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddTabletopCardEngineInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSource(HoldemEngineTelemetry.ActivitySourceName);
    }

    /// <summary>
    /// Subscribes the meter provider to the engine's <c>Meter</c>: the action, hand and
    /// validation-failure counters and the action duration histogram. The engine's telemetry is
    /// described in <c>docs/observability.md</c>.
    /// </summary>
    /// <param name="builder">The meter provider builder of the host.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static MeterProviderBuilder AddTabletopCardEngineInstrumentation(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddMeter(HoldemEngineTelemetry.MeterName);
    }

    /// <summary>
    /// Subscribes both the tracer provider and the meter provider to the engine's
    /// <c>ActivitySource</c> and <c>Meter</c> in a single call. The host wires both signals with:
    /// <code>services.AddOpenTelemetry().AddTabletopCardEngineInstrumentation()</code>
    /// chained with <c>WithTracing()</c> and <c>WithMetrics()</c> to add exporters. The engine's
    /// telemetry is described in <c>docs/observability.md</c>.
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder of the host.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IOpenTelemetryBuilder AddTabletopCardEngineInstrumentation(this IOpenTelemetryBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddTabletopCardEngineInstrumentation());
        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddTabletopCardEngineInstrumentation());
        return builder;
    }
}
