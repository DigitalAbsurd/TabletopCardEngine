namespace OpenHoldemEngine.Engine;

/// <summary>
/// One entry in a seat's betting record for the street in progress.
/// </summary>
/// <param name="Action">
/// One of the five actions the betting rules distinguish — <see cref="HandActionType.SmallBlind"/>,
/// <see cref="HandActionType.BigBlind"/>, <see cref="HandActionType.Fold"/>,
/// <see cref="HandActionType.Call"/>, <see cref="HandActionType.Raise"/>.
/// </param>
/// <param name="Amount">The seat's cumulative wager for the street after this action, not the increment.</param>
/// <param name="AddAmount">How much this action raised the street wager by; 0 for anything but a raise or a blind.</param>
/// <param name="Paid">Chips this action actually took out of the stack.</param>
internal readonly record struct SeatBet(HandActionType Action, decimal Amount, decimal AddAmount, decimal Paid);
