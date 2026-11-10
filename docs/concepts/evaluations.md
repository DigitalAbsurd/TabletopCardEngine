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

### Raise under Pot-Limit: the maximum is the pot

In a No-Limit game (`BettingStructure.NoLimit`) the `Raise`'s `MaxValue` is the seat's whole stack: its street bet plus what it has left. In a Pot-Limit game (`BettingStructure.PotLimit`) `MaxValue` is the standing wager plus the pot as it would be once the seat has called, and never more than the stack:

```
MaxValue = StandingWager + (Pot + StreetPot + MoreToCall)   // then capped at BetSize + Stack
```

`Pot + StreetPot` is every chip committed this hand, blinds and the wagers of seats that have since folded included. At blinds 1/2 with three seats of 200, the first seat to act may raise to `2 + (3 + 2) = 7`; after it raises to 7, the small blind may raise to `7 + (10 + 6) = 23`. The minimum is the same as in No-Limit.

Under Pot-Limit an all-in raise is offered on its own (`[stack-stack]`) only when the stack cannot reach the minimum raise. An `AllIn` whose total is above `MaxValue` fails with `IllegalAction`, as does a `Raise` above it.

### Raise under Fixed-Limit: one fixed total

In a Fixed-Limit game (`BettingStructure.FixedLimit`) every bet and raise is one fixed size, so the `Raise` window is a single value: `MinValue == MaxValue ==` the standing wager plus one bet. The bet is the hand's big blind pre-flop and on the flop (the small bet) and twice that on the turn and river (the big bet). It follows the hand's blinds, so a blind schedule raises the bets with them, and a big blind posted short by a small stack does not lower it.

At blinds 1/2 the first raise pre-flop is to 4 and the next to 6; the first bet on the flop is 2; on the turn a bet is 4 and a raise over it goes to 8. A seat whose stack falls short of the total but covers more than the call is offered the all-in raise instead (`[stack-stack]`, the street bet included).

A street holds at most `GameOptionData.RaiseCap` bets (4 by default, `null` for no cap). Pre-flop the big blind is the first. Every bet and raise counts, and so does an all-in raise that lifts the standing wager by at least half a bet; a smaller all-in raise does not. Once the cap is reached there is no `Raise` entry, and an `AllIn` worth more than the call fails with `IllegalAction`, as does any `Raise` or `AllIn` past the window.

An all-in raise of at least half a bet also reopens the betting (see below): on the turn at blinds 1/2, after a bet of 4, an all-in to 6 reopens it for the bettor and the next raise goes to 10, while an all-in to 5 does neither and the next raise goes to 9.

### Why the difference?

A "call" button in a UI wants to know "put this many more chips in". A "raise to" slider wants to know "drag to this value". The engine provides exactly that, so your UI does not need to translate.

## Rules for all actions

- **`Fold`**: Always offered (it is always legal). Window is empty (`[0-0]`), because no size applies.
- **`Check`**: Offered only when there is nothing to call (standing wager is 0 or equals the seat's current bet). Window is empty.
- **`Call`**: Offered when there is something to call and the stack can cover it. Window is an increment.
- **`Raise`**: Window is both an absolute total (min and max street total to raise to). The minimum is the standing wager plus the last full raise, and a full raise is never less than one big blind (so the first raise pre-flop is to two big blinds). See [The hand ledger](hand-ledger.md) for how the engine calculates the minimum.
- **`Bet`**: Offered only as the opening wager on a street where nothing has been bet. Window is an increment.
- **`AllIn`**: Commits the whole stack. It is a call (if the stack does not exceed the amount to call), which is always legal, or a raise (if it does), which is legal only where raising is (see below) and, under Pot-Limit, only when the stack is within the pot limit, or, under Fixed-Limit, only when the stack is at most the one raise-to total. Window is empty because the amount is fixed.

## When the betting is not reopened

An all-in for less than a full raise does not reopen the betting to a seat that has already acted on the street (TDA Rule 47). Such a seat is offered `Fold` and `Call` only: no `Raise` window and no all-in raise, and a `Raise`, or an `AllIn` that would be more than a call, fails with `IllegalAction`. The betting reopens for it once the standing wager has risen by at least a full raise since its last action, counted cumulatively, so two short all-ins that add up to a full raise do reopen it. Posting a blind is not acting, so the big blind keeps its option to raise when everyone limps. Under Fixed-Limit the threshold is half a bet instead of a full raise (TDA Rule 47 for limit games).

Example at blinds 1/2: seat 1 raises to 10, seat 2 calls, seat 3 goes all in for 13, three more than 10 and short of the full raise of 8. Seat 1 is offered `Fold` and `Call 3` only. Had seat 3 gone all in for 30, seat 1 could raise again.

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
| Raise  | 10  | 100 | Raise the street total to 10 (the minimum: the standing 6 plus the last full raise of 4) up to 100 (all in: the 98 stack plus the 2 already bet) |

There is no `AllIn` row: going all in is a `Raise` to the top of the window, here 100.

**Making a raise call with Play:**

To raise the street wager to 20 (putting in 18 more chips: 20 - 2 = 18):

```csharp
var play = HoldemAction.Play(gameId, PlayerActionType.Raise, size: 20);
var result = await engine.ApplyAsync(play, ct);
```

Notice that the `size` parameter is 20 (the absolute street total), not 18 (the increment). The window says "raise to 10–100", so 20 is in range. The engine records the action and updates:

- Seat 1's `BetSize` becomes 20
- Seat 1's stack becomes 80 (98 - 18)
- `StreetPot` now includes Seat 1's 20

If you sent `size: 100`, the engine would raise to 100 (the maximum, putting in the whole stack). If you sent `size: 9` (outside the window), the engine would reject with `IllegalAction`.

## Call vs Raise: the key difference

| Action | Window basis | Example: standing wager is 10 |
|--------|---|---|
| Call   | Increment ("how much more") | `[5-5]`: put in 5 more; end up at 10 total |
| Raise  | Absolute street total | `[12-50]`: raise to 12, 13, …, or 50; put in 2–40 more |

This difference is by design. A call is easy to visualize as a top-up, a raise as "drag the slider to the amount you want the street total to be".

## Reading Evaluations correctly

When you read `GameData.Evaluations`:

1. If `Evaluations` is empty, it is nobody's turn (hand is over, or not yet started).
2. `Fold` is always in the list while it is a seat's turn, with an empty window (`[0-0]`).
3. For `Call`, add `MinValue` to the seat's current `BetSize` to get the absolute amount.
4. For `Raise`, the window is already absolute; use it directly.
5. `AllIn` commits the whole stack. As a call it is always legal; as a raise it is legal only when `Evaluations` offers a `Raise`.

See also: [The hand ledger](hand-ledger.md)
