# Legal actions and bet sizing

When it is a seat's turn to act, `HandData.Evaluations` lists every legal action and its sizing window. These evaluations are the engine's answer to "what can this seat do right now?"

## Evaluations and their windows

```csharp
public record PlayerActionEvaluationData(
    PlayerActionType PlayerActionType, 
    decimal MinValue = 0, 
    decimal MaxValue = 0);
```

Each evaluation describes one legal action:

- **`PlayerActionType`**: The action the seat may take (Fold, Check, Call, Raise, Bet, AllIn).
- **`MinValue` / `MaxValue`**: The sizing window for the action — but on **different bases** by design.

### Call: increments, not absolutes

For `Call`, `MinValue` and `MaxValue` are **how much more the seat must put in**:

```
MoreToCall = CurrentStandingWager - SeatCurrentStreetBet
```

If a `Call` evaluation shows `[1-1]`, the seat must put in exactly 1 more chip. The absolute amount it will have committed on the street is not in the window — calculate it by adding to its current bet.

### Raise: absolute street totals

For `Raise`, both bounds are **the total street wager to raise to** (the same basis `HoldemAction.Play(gameId, PlayerActionType.Raise, size)` expects):

```
RaiseToThis = eval.MinValue  // Raise the street wager to this amount
```

If a `Raise` evaluation shows `[3-100]`, the seat may raise the street wager to any value from 3 to 100. The increment the seat puts in is not in the window — calculate it by subtracting its current bet from the chosen total.

### Why the difference?

A "call" button in a UI wants to know "put this many more chips in". A "raise to" slider wants to know "drag to this value". The engine provides exactly that, so your UI does not need to translate.

## Rules for all actions

- **`Fold`**: Always offered (it is always legal). Window is empty (`[0-0]`), because no size applies.
- **`Check`**: Offered only when there is nothing to call (standing wager is 0 or equals the seat's current bet). Window is empty.
- **`Call`**: Offered when there is something to call and the stack can cover it. Window is an increment.
- **`Raise`**: Window is both an absolute total (min and max street total to raise to). Minimum is at least 1 more than the standing wager. See [The hand ledger](hand-ledger.md) for how the engine calculates the minimum.
- **`Bet`**: Offered only as the opening wager on a street where nothing has been bet. Window is an increment.
- **`AllIn`**: Always legal and commits the whole stack. It is a call (if the stack does not exceed the amount to call) or a raise (if it does). Window is empty because the amount is fixed.

## Rejecting illegal actions

The engine rejects any action not listed in `Evaluations`, or a raise outside the window, with `HoldemErrorCode.IllegalAction`. The call fails and nothing is persisted.

The engine never corrects an action into another one. If you send a raise to an illegal amount, it fails immediately. If you send a call when only a raise is legal, it fails — you cannot "correct" it to the minimum raise.

## Worked example: Decision point with real numbers

Three seats at blinds 1/2 on a table with 100 stacks each. The action so far:

- Seat 2 posted small blind (1)
- Seat 3 posted big blind (2)
- Seat 1 called (1 more, total 2 on this street)
- Seat 2 raised to 6 (5 more)
- Seat 3 calls the 6 (4 more)
- Seat 1's turn again

**Current state for Seat 1:**

- Stack: 98
- Current street bet (`BetSize`): 2 (posted earlier in the same street)
- Standing wager: 6 (max any seat has wagered this street)
- `MoreToCall`: 6 - 2 = 4

**Evaluations offered to Seat 1:**

| Action | Min | Max | Interpretation |
|--------|-----|-----|---|
| Fold   | 0   | 0   | Give up; no chips used |
| Call   | 4   | 4   | Put in 4 more chips; total 6 on this street |
| Raise  | 8   | 98  | Raise the street total to 8 (the minimum, 2× the big blind) up to 98 (all in) |
| AllIn  | 0   | 0   | Commit all 98 chips |

**Making a raise call with Play:**

To raise the street wager to 20 (putting in 18 more chips: 20 - 2 = 18):

```csharp
var play = HoldemAction.Play(gameId, PlayerActionType.Raise, size: 20);
var result = await engine.ApplyHoldemActionAsync(play, ct);
```

Notice that the `size` parameter is 20 (the absolute street total), not 18 (the increment). The window says "raise to 8–98", so 20 is in range. The engine records the action and updates:

- Seat 1's `BetSize` becomes 20
- Seat 1's stack becomes 80 (98 - 18)
- `StreetPot` now includes Seat 1's 20

If you sent `size: 98`, the engine would raise to 98 (the maximum, putting in the whole stack). If you sent `size: 7` (outside the window), the engine would reject with `IllegalAction`.

## Call vs Raise: the key difference

| Action | Window basis | Example: standing wager is 10 |
|--------|---|---|
| Call   | Increment ("how much more") | `[5-5]`: put in 5 more; end up at 10 total |
| Raise  | Absolute street total | `[12-50]`: raise to 12, 13, …, or 50; put in 2–40 more |

This difference is by design. A call is easy to visualize as a top-up, a raise as "drag the slider to the amount you want the street total to be".

## Reading Evaluations correctly

When you read `GameData.Evaluations`:

1. If `Evaluations` is empty, it is nobody's turn (hand is over, or not yet started).
2. All seats can fold. `Fold` may not appear in the list, but it is always an option.
3. For `Call`, add `MinValue` to the seat's current `BetSize` to get the absolute amount.
4. For `Raise`, the window is already absolute; use it directly.
5. `AllIn` is always legal and requires no additional checks; it commits the whole stack.

See also: [The hand ledger](hand-ledger.md)
