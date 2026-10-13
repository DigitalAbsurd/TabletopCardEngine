using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace TabletopCardEngine.Holdem.Diagnostics;

/// <summary>The Meter and instruments the engine publishes metrics to.</summary>
internal sealed class HoldemMetrics
{
    /// <summary>The process-wide fallback meter, used when no IMeterFactory is available.</summary>
    private static readonly Meter _fallbackMeter = new(
        TabletopCardEngineTelemetry.MeterName,
        typeof(HoldemMetrics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>Counts the number of actions applied, one per call, including failures.</summary>
    private readonly Counter<long> _actionCounter;

    /// <summary>Counts the number of hands started.</summary>
    private readonly Counter<long> _handCounter;

    /// <summary>Counts the number of validation failures.</summary>
    private readonly Counter<long> _validationFailureCounter;

    /// <summary>Records the duration of each action in seconds.</summary>
    private readonly Histogram<double> _actionDurationHistogram;

    /// <summary>Create the meter and all four instruments.</summary>
    internal HoldemMetrics(Meter meter)
    {
        _actionCounter = meter.CreateCounter<long>(
            "tabletopcardengine.actions",
            unit: "{action}",
            description: "Count of actions applied, one per call with outcome recorded.");

        _handCounter = meter.CreateCounter<long>(
            "tabletopcardengine.hands",
            unit: "{hand}",
            description: "Count of hands started.");

        _validationFailureCounter = meter.CreateCounter<long>(
            "tabletopcardengine.validation_failures",
            unit: "{failure}",
            description: "Count of validation failures, one per error including conflicts.");

        _actionDurationHistogram = meter.CreateHistogram<double>(
            "tabletopcardengine.action.duration",
            unit: "s",
            description: "Duration of each action in seconds from start to end, exceptions included.");
    }

    /// <summary>Get or create a metrics instance backed by the factory-created meter, or fall back to the process-wide meter.</summary>
    internal static HoldemMetrics CreateOrGetFallback(IMeterFactory? meterFactory)
    {
        if (meterFactory is not null)
        {
            var meter = meterFactory.Create(new MeterOptions(TabletopCardEngineTelemetry.MeterName)
            {
                Version = typeof(HoldemMetrics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            });
            return new HoldemMetrics(meter);
        }

        return new HoldemMetrics(_fallbackMeter);
    }

    /// <summary>Record one action with the outcome and action type tags.</summary>
    internal void RecordAction(string actionType, string outcome)
    {
        var tags = new TagList
        {
            { HoldemActivity.ActionTypeTag, actionType },
            { HoldemActivity.OutcomeTag, outcome }
        };
        _actionCounter.Add(1, tags);
    }

    /// <summary>Record one hand started.</summary>
    internal void RecordHandStarted()
    {
        _handCounter.Add(1);
    }

    /// <summary>Record one validation failure with the action type and error code tags.</summary>
    internal void RecordValidationFailure(string actionType, HoldemErrorCode errorCode)
    {
        var tags = new TagList
        {
            { HoldemActivity.ActionTypeTag, actionType },
            { HoldemActivity.ErrorCodeTag, errorCode.ToString() }
        };
        _validationFailureCounter.Add(1, tags);
    }

    /// <summary>Record action duration in seconds with the action type and outcome tags.</summary>
    internal void RecordActionDuration(string actionType, string outcome, double seconds)
    {
        var tags = new TagList
        {
            { HoldemActivity.ActionTypeTag, actionType },
            { HoldemActivity.OutcomeTag, outcome }
        };
        _actionDurationHistogram.Record(seconds, tags);
    }
}
