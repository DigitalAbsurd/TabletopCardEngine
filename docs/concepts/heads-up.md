# Heads-up play

When a game shrinks to two players, or when it is created with a maximum of two seats, the table structure changes fundamentally. The position, the order of play, and the blind rules are all different from a ring game. This page explains how two-player poker works in the engine.

## When heads-up applies

Heads-up occurs when exactly two seats have chips at the start of a hand, whether the game was created for two players or shrank to two through eliminations. It is not a shorter ring — it is a different structure with its own position rules.

In `HandData`, the distinguishing mark is `DealerSeatNo == SmallBlindSeatNo` — the button posts the small blind instead of the seat to its left.

## Position and the order of play

In heads-up, the button takes both the small blind and a special acting role:

- **Pre-flop**: The button posts the small blind, acts *first*, and must act again if the big blind raises.
- **Flop, Turn, River**: The button posts the small blind (standing from pre-flop), acts *last*, and the big blind acts first.

This reversal of position between pre-flop and post-flop is the core of heads-up strategy. The player on the button has information advantage post-flop (acting after seeing the big blind's decision) but a disadvantage pre-flop (committing money without seeing a raise).

### In GameData

The `DealerSeatNo`, `SmallBlindSeatNo`, and `BigBlindSeatNo` in `HandData` name the seats correctly for heads-up:

```csharp
// Heads-up hand
var handData = gameData.HandData!;
var dealerSeat = handData.DealerSeatNo;    // 1 (or any first seat number)
var smallBlindSeat = handData.SmallBlindSeatNo;  // Same as dealer — the button
var bigBlindSeat = handData.BigBlindSeatNo;     // 2 (or the other seat)

// Use NextPlayerSeatNo to see who is on the clock
// Pre-flop: NextPlayerSeatNo == DealerSeatNo (button acts first)
// Flop+: NextPlayerSeatNo == BigBlindSeatNo (big blind acts first)
```

## The big blind's option pre-flop

When the button calls the big blind pre-flop (matching the big blind without raising), the big blind retains the right to raise or check. This is the **big blind's option** or "option on the river" — a special heads-up rule.

In `HandData.Evaluations`, when the button has only called and the big blind faces the next decision, the big blind is offered `Fold`, `Check`, and `Raise`, not just `Check` or `Fold`. The big blind can exercise this option or check and move to the flop.

## The button alternates every hand

In a ring game, the button walks around the table (seat 1 → 2 → 3 → 1). In heads-up, the same two seats play every hand, so the button must alternate between them to keep the position balanced:

- Hand 1: Seat 1 posts small blind, acts first pre-flop
- Hand 2: Seat 2 posts small blind, acts first pre-flop
- Hand 3: Seat 1 again
- ...

The button switches every `StartHand`, so the players alternate between a disadvantage pre-flop and an advantage post-flop.

## A blind post is capped at the stack

When a seat cannot cover its blind, it posts what it has and becomes all-in immediately. The blind is not covered from the seat's buy-in or external funds — it is capped at the stack available.

- If the small blind posts but the remaining stack is less than the big blind, the big blind is uncalled and comes back to the small blind.
- If the big blind posts but the remaining stack is zero, the hand proceeds as a side pot and the big blind seat is all-in.

In the `HandAction` ledger, `Amount` records what was actually posted, while `Size` records the nominal blind (see [the hand ledger](hand-ledger.md#understanding-amount)). Heads-up is where this happens most often, because stacks below one big blind are common rather than rare.

## Worked example: a heads-up hand, street by street

Here is the play of one heads-up hand with small blind 50 and big blind 100:

```
Starting stacks: Seat 1 (button) = 10,000, Seat 2 (big blind) = 10,000

Hand 1, starting dealer/button: Seat 1

PreFlop:
  DealerSeatNo=1, SmallBlindSeatNo=1, BigBlindSeatNo=2, NextPlayerSeatNo=1
  - Seat 1 posts 50 (small blind)
  - Seat 2 posts 100 (big blind)
  - Seat 1 (button) acts first: calls 100 (puts in 50 more to match the 100)
  - Seat 2 (big blind) gets the option: checks
  - Street pot: 200

Flop: [4♠ 9♥ Q♦]
  DealerSeatNo=1, SmallBlindSeatNo=1, BigBlindSeatNo=2, NextPlayerSeatNo=2
  - Seat 2 (big blind) acts first: checks
  - Seat 1 (button) acts last: checks
  - Street pot: 200

Turn: [4♠ 9♥ Q♦ 3♣]
  DealerSeatNo=1, SmallBlindSeatNo=1, BigBlindSeatNo=2, NextPlayerSeatNo=2
  - Seat 2 acts first: checks
  - Seat 1 acts last: checks
  - Street pot: 200

River: [4♠ 9♥ Q♦ 3♣ 8♠]
  DealerSeatNo=1, SmallBlindSeatNo=1, BigBlindSeatNo=2, NextPlayerSeatNo=2
  - Seat 2 acts first: checks
  - Seat 1 acts last: checks
  - Hand goes to showdown, both hand cards are revealed and compared

Showdown:
  - Seat 1 wins the pot of 200 (the pair on the board breaks ties)
  - Seat 1 stack: 10,000 + 100 (net) = 10,100
  - Seat 2 stack: 10,000 − 100 (net) = 9,900

Hand 2, starting dealer/button: Seat 2
  - Seat 2 now posts the small blind
  - Seat 1 now posts the big blind
  - Play reverses positions for the next hand
```

Notice that `DealerSeatNo` and `SmallBlindSeatNo` are always the same, and `NextPlayerSeatNo` equals the dealer pre-flop but switches to the big blind on the flop.

## See also

- **[The hand ledger](hand-ledger.md)** — How blinds are recorded when they do not cover the full nominal amount.
- **[Legal actions and bet sizing](evaluations.md)** — Understanding why the big blind's option appears in `Evaluations`.
