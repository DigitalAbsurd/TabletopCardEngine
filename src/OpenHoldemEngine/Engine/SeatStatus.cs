namespace OpenHoldemEngine.Engine;

/// <summary>Where a seat stands in the current hand.</summary>
internal enum SeatStatus : byte
{
    /// <summary>Still has chips behind and still owes decisions.</summary>
    Waiting = 0,

    /// <summary>Everything is in the middle; the seat contests the pot but is never asked again.</summary>
    AllIn = 1,

    /// <summary>Out of the hand.</summary>
    Folded = 2
}
