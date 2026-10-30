using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Diagnostics;

/// <summary>Names a command for logs, traces and metrics, so they all spell it the same way.</summary>
internal static class HoldemActionNames
{
    /// <summary>The verb of a command: its record type name without the <c>HoldemAction</c> prefix, or <c>Unknown</c> for a type the engine does not know.</summary>
    internal static string Of(HoldemAction action) => action switch
    {
        HoldemActionCreateGame => "CreateGame",
        HoldemActionGetGame => "GetGame",
        HoldemActionStartGame => "StartGame",
        HoldemActionStartHand => "StartHand",
        HoldemActionPlay => "Play",
        HoldemActionFinishGame => "FinishGame",
        HoldemActionLeaveSeat => "LeaveSeat",
        _ => "Unknown"
    };
}
