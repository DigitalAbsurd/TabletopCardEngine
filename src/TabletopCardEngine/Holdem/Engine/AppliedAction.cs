namespace TabletopCardEngine.Holdem.Engine;

/// <summary>What happened when one player action was applied to a <see cref="HandTable"/>.</summary>
/// <param name="SeatNo">The seat that acted. Seats are 1-based.</param>
/// <param name="Action">What the rules actually recorded — a request the rules could not honor is reported as a fold.</param>
/// <param name="Amount">The seat's cumulative wager for the street after this action.</param>
/// <param name="AddAmount">How much this action added on top of the seat's previous wager.</param>
/// <param name="Paid">Chips that actually left the stack, which is less than <paramref name="AddAmount"/> on a short all-in.</param>
/// <param name="Street">The street the action was taken on.</param>
/// <param name="NextStreet">The street now in progress; differs from <paramref name="Street"/> when this action closed one.</param>
/// <param name="NextSeatNo">The seat to act next, or <c>0</c> when it is nobody's turn.</param>
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
