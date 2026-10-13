namespace TabletopCardEngine;

/// <summary>
/// The telemetry sources declared by the Holdem engine for OpenTelemetry hosts.
/// </summary>
public static class TabletopCardEngineTelemetry
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> the engine publishes to.
    /// Pass this to <c>TracerProviderBuilder.AddSource</c> when wiring up OpenTelemetry.
    /// </summary>
    public const string ActivitySourceName = "TabletopCardEngine";

    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> the engine publishes to.
    /// Pass this to <c>MeterProviderBuilder.AddMeter</c> when wiring up OpenTelemetry.
    /// </summary>
    public const string MeterName = "TabletopCardEngine";
}
