using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Engine;

/// <summary>What happened when one player action was applied to a <see cref="HandTable"/>.</summary>
/// <param name="Action">What the rules actually recorded — a request the rules could not honor is reported as a fold.</param>
/// <param name="Board">Community cards dealt by this action, in order; empty unless a street (or several, on a run-out) opened.</param>
/// <param name="PrizeBySeatNo">Non-null once the pot has been awarded — the hand is over.</param>
internal sealed record AppliedAction(
    byte SeatNo,
    PlayerActionType Action,
    decimal Amount,
    decimal AddAmount,
    decimal Paid,
    Street Street,
    Street NextStreet,
    byte NextSeatNo,
    IReadOnlyList<Card> Board,
    IReadOnlyDictionary<byte, decimal>? PrizeBySeatNo);
