namespace OpenHoldemEngine;

/// <summary>
/// The telemetry sources declared by the Holdem engine for OpenTelemetry hosts.
/// </summary>
public static class HoldemEngineTelemetry
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> the engine publishes to.
    /// Pass this to <c>TracerProviderBuilder.AddSource</c> when wiring up OpenTelemetry.
    /// </summary>
    public const string ActivitySourceName = "OpenHoldemEngine";
}
