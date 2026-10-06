# The hand ledger

Every action a seat takes during a hand — blinds, bets, calls, raises, folds, and checks — is recorded in the `HandAction` table. This ledger is the complete history of the hand and is exposed to consumers through `HandData.Actions` as a list of `PlayerActionData` records.

## The ledger structure

Each row in the ledger records one action:

```csharp
public record PlayerActionData(
    Street Street,
    HandActionType ActionType, 
    decimal Size, 
    int No)
{
    public byte SeatNo { get; init; }
    public decimal Amount { get; init; }
}
```

- **`No`**: Position in the hand, ascending from 1. Action 1 is always the small blind.
- **`SeatNo`**: The seat that took the action (1-based; 0 means "no seat").
- **`Street`**: The betting round (PreFlop=0, Flop=3, Turn=4, River=5, Showdown=6, Finished=7).
- **`ActionType`**: What the seat did. The ledger records `HandActionType`, which is what the caller asked for plus the two forced blind posts:
  - `SmallBlind` and `BigBlind` (engine places these, never requested)
  - `Fold`, `Check`, `Call`, `Raise`, `Bet`, `AllIn` (caller may request)
- **`Size`**: What the caller asked for. For blinds, this is the nominal blind amount. For a raise, it is the street total to raise to. For calls, checks, and folds, it is 0.
- **`Amount`**: The seat's **cumulative** wager on this street **after** the action. This is the key field for calculating pots.

## Understanding Amount

The `Amount` field is cumulative per (SeatNo, Street) pair. It is:

- For a blind: what was actually posted (which may be less than the nominal blind if the stack was short)
- For a call or check: the standing wager on that street
- For a raise or bet: the new street total
- For a fold: 0 (the seat is no longer contesting the pot)

To find how much a seat wagered on a street, take the maximum `Amount` for that seat and street.

## From ledger to pots

The `HandData.StreetPot` and `HandData.Pot` fields derive directly from the ledger:

- **`StreetPot`**: The total wagered on the current street. For each active seat, take the max `Amount` for the current street; sum them (but count each seat only once, even if it folded). This is the pot in the middle waiting for the current action.
- **`Pot`**: Everything wagered on prior streets. Sum the same way, but for all streets before the current one.
- **Total in play**: `Pot + StreetPot`

For a seat, **`BetSize`** (on `SeatData`) is that seat's contribution to the current street — its max `Amount` for the current street, or null if it has not acted yet.

## ActionType vs PlayerActionType

- **`PlayerActionType`** (in `HoldemAction.Play`) is what a caller may request: Fold, Check, Call, Raise, Bet, AllIn.
- **`HandActionType`** (in the ledger) is what was recorded: the same values plus SmallBlind and BigBlind (the engine places these, not the caller).
- **`AllIn`** is always legal and commits the whole stack. It is recorded as `AllIn` in the ledger, not translated to a raise or call.

## Worked example: Pre-flop sequence

Three seats at blinds 1/2, each starting with 100:

| Action | Seat | Type     | Size | Amount | Street  | Notes                                           |
|--------|------|----------|------|--------|---------|--------------------------------------------------|
| 1      | 2    | SmallBlind | 1   | 1      | PreFlop | Seat 2 posts the forced small blind            |
| 2      | 3    | BigBlind   | 2   | 2      | PreFlop | Seat 3 posts the forced big blind              |
| 3      | 1    | Call       | 0   | 2      | PreFlop | Seat 1 calls the big blind; now has 98 in     |
| 4      | 2    | Raise      | 6   | 6      | PreFlop | Seat 2 raises to 6 total; puts in 5 more      |
| 5      | 3    | Call       | 0   | 6      | PreFlop | Seat 3 calls the raise; puts in 4 more        |
| 6      | 1    | Fold       | 0   | 0      | PreFlop | Seat 1 folds out                               |

After this sequence:

- **StreetPot** = 16 (max per seat: seat 2 has 6, seat 3 has 6, seat 1 has 0 because it folded)
- **Pot** = 0 (no prior streets)
- **Seat 1 BetSize** = null (it folded, so it is no longer active on this street)
- **Seat 2 BetSize** = 6
- **Seat 3 BetSize** = 6
- **Seat 1 Stack** = 98 (wagered 2 before folding)
- **Seat 2 Stack** = 94 (wagered 6)
- **Seat 3 Stack** = 94 (wagered 6)

**Note on raises and bet sizing**: A raise's `Size` is the **street total to raise to**, not the increment. So action 4 (`Size = 6`) means "raise the street wager to 6", not "raise by 6". The engine's `BettingRules.MinRaiseAmount` calculates minimums based on the last raise's increment, so the minimum might not be a power of 2; follow what the engine offers in `HandData.Evaluations`.

## Reading GameData correctly

When you read `GameData` back from the engine, remember:

1. `HandData.Actions` includes every action, blinds included, in order.
2. `Amount` is cumulative per seat per street; don't assume it is the increment.
3. `Pot` is prior streets only; the money in play right now is `Pot + StreetPot`.
4. `SeatData.BetSize` is the seat's current-street wager, or null if it hasn't acted yet.
5. A fold sets `Amount` to 0 in the ledger and removes the seat from `StreetPot` calculations.

See also: [Legal actions and bet sizing](evaluations.md)
