using System.Diagnostics;
using System.Reflection;

namespace OpenHoldemEngine.Diagnostics;

/// <summary>The ActivitySource and tag names for engine telemetry.</summary>
internal static class HoldemEngineActivity
{
    /// <summary>The engine's ActivitySource, opened with the assembly's informational version.</summary>
    internal static readonly ActivitySource Source = new(
        HoldemEngineTelemetry.ActivitySourceName,
        typeof(HoldemEngineActivity).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>The span name for each <see cref="HoldemEngine.ApplyHoldemActionAsync"/> call.</summary>
    internal const string Name = "OpenHoldemEngine.ApplyHoldemAction";

    /// <summary>The action type: <c>CreateGame</c>, <c>GetGame</c>, <c>StartGame</c>, <c>StartHand</c>, <c>Play</c>, <c>FinishGame</c>, or <c>Unknown</c>.</summary>
    internal const string ActionTypeTag = "openholdemengine.action.type";

    /// <summary>For <c>Play</c> actions only: the player action type (<c>Fold</c>, <c>Check</c>, <c>Call</c>, <c>Raise</c>, <c>Bet</c>, <c>AllIn</c>).</summary>
    internal const string PlayerActionTypeTag = "openholdemengine.player_action.type";

    /// <summary>The game ID.</summary>
    internal const string GameIdTag = "openholdemengine.game.id";

    /// <summary>The current hand's number (omitted when <c>0</c>).</summary>
    internal const string HandNumberTag = "openholdemengine.hand.no";

    /// <summary>The current street of the current hand after the call: <c>PreFlop</c>, <c>Flop</c>, <c>Turn</c>, <c>River</c>, <c>Showdown</c>, or <c>Finished</c> (omitted if no hand).</summary>
    internal const string StreetTag = "openholdemengine.street";

    /// <summary><c>Success</c>, <c>Failure</c> (including a <c>Conflict</c>), or <c>Exception</c>.</summary>
    internal const string OutcomeTag = "openholdemengine.outcome";

    /// <summary>For a <c>Failure</c> outcome: the error code name.</summary>
    internal const string ErrorCodeTag = "openholdemengine.error.code";
}
