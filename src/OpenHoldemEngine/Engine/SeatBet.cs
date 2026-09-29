namespace OpenHoldemEngine.Engine;

/// <summary>
/// One entry in a seat's betting record for the street in progress.
/// </summary>
/// <param name="Action">
/// One of the five actions the betting rules distinguish — <see cref="PlayerActionType.SmallBlind"/>,
/// <see cref="PlayerActionType.BigBlind"/>, <see cref="PlayerActionType.Fold"/>,
/// <see cref="PlayerActionType.Call"/>, <see cref="PlayerActionType.Raise"/>.
/// </param>
/// <param name="Amount">The seat's cumulative wager for the street after this action, not the increment.</param>
/// <param name="AddAmount">How much this action raised the street wager by; 0 for anything but a raise or a blind.</param>
/// <param name="Paid">Chips this action actually took out of the stack.</param>
internal readonly record struct SeatBet(PlayerActionType Action, decimal Amount, decimal AddAmount, decimal Paid);
