# Buy-ins, top-ups, blinds and antes

A cash game is more than the hands: players sit down with money, add to it, buy back in after a bust and get removed, and the stakes may rise as the game goes on. This page covers the options and actions that control money in and out of a seat, the blind schedule and the antes. Every option has a default that gives the plain game of 1.1, so a caller that sets none of them sees no change.

All of them are set once, when the game is created, on [`GameOptionData`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.Dto.GameOptionData.md).

## The buy-in window

`MinBuyIn` and `MaxBuyIn` bound the amount a player may bring to the table. Both are optional (`null` means no limit), must be positive with at most two decimal places, and `MinBuyIn` may not exceed `MaxBuyIn`; otherwise `CreateGame` fails with `HoldemErrorCode.InvalidGameOptions`.

The window applies to three things, and a violation is always `HoldemErrorCode.InvalidBuyIn`:

- **A join**: `PlayerJoinData.BuyInAmount` in `StartGame` or `StartHand` must lie in `[MinBuyIn, MaxBuyIn]`.
- **A rebuy**: the amount must lie in `[MinBuyIn, MaxBuyIn]` too.
- **A top-up**: there is no minimum, but the seat's stack after the top-up may not exceed `MaxBuyIn`.

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    InitialSmallBlind = 1,
    MinBuyIn = 40,
    MaxBuyIn = 200,
}), ct);
var gameId = created.Value!.GameId;

var tooSmall = await engine.ApplyAsync(HoldemAction.StartGame(gameId, new[]
{
    new PlayerJoinData { SeatNo = 1, Name = "Alice", BuyInAmount = 20 },
    new PlayerJoinData { SeatNo = 2, Name = "Bob", BuyInAmount = 100 },
}), ct);
// tooSmall.Errors[0].Code == HoldemErrorCode.InvalidBuyIn
```

## Top-up and rebuy

Adding chips to a seat is one action, `HoldemAction.TopUp(gameId, seatNo, amount)`. Which kind it is depends on the seat's stack, not on anything the caller says: if the stack the seat would start the next hand with is above zero it is a **top-up**; if it is zero it is a **rebuy**.

- It is allowed **between hands only**. Before the first hand it fails with `GameNotStarted`, and during a hand with `PreviousHandNotEnded`.
- The chips do not go into the stack at once. They wait as `SeatData.PendingBuyIn` and are dealt in at the start of the next hand. A second call between the same hands adds to the pending amount. A seat with only a pending rebuy still counts as funded when the engine checks that two players can play (`NotEnoughPlayers`).
- A top-up needs `AllowTopUp`, otherwise it fails with `HoldemErrorCode.TopUpNotAllowed`.
- A rebuy needs `AllowRebuy`, otherwise it fails with `HoldemErrorCode.RebuyNotAllowed`. `MaxRebuys` caps how many rebuys each player makes (`null` is unlimited, and it can be set only together with `AllowRebuy`). `SeatData.Rebuys` counts the rebuys of the seat's current player; a new player in the same seat starts from zero, and top-ups are not counted.
- A seat that is empty or whose player has left fails with `InvalidSeat`, and an amount that is zero, negative, has more than two decimal places or falls outside the buy-in window fails with `InvalidBuyIn`.
- `LeaveSeat`, `KickSeat` and `FinishGame` cancel a pending amount: those chips were never in play, so they are not part of the stack that leaves.

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    AllowTopUp = true,
    AllowRebuy = true,
    MaxRebuys = 2,
    MaxBuyIn = 200,
}), ct);
var gameId = created.Value!.GameId;

// ... StartGame, play a hand to the end ...

// Alice still has chips: this is a top-up, and it waits for the next hand.
var topUp = await engine.ApplyAsync(HoldemAction.TopUp(gameId, 1, 50), ct);
var pending = topUp.Value!.HandData!.Seats.Single(s => s.SeatNo == 1).PendingBuyIn; // 50

await engine.ApplyAsync(HoldemAction.StartHand(gameId), ct); // the 50 is in her stack now
```

See [`HoldemActionTopUp`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.md) and [`SeatData`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.Dto.SeatData.md).

## Kick

`HoldemAction.KickSeat(gameId, seatNo)` removes a player at any time. The engine does not decide who may call it: authorising the caller is the host's job, and there is no flag on the game for it.

- **Between hands** it does exactly what `LeaveSeat` does: the stack leaves with the player and the seat is free for the next hand.
- **During a hand** the seat is folded at once, out of turn if need be, and the player leaves after the hand. Whoever is on the clock stays on the clock. If the fold leaves one seat contesting the pot, that seat wins it in the ordinary way.
- A seat that has already folded or is **all in** gets no fold: an all-in seat stays in the pot to showdown and takes its winnings away with it.
- A kicked seat, an empty seat or a seat that has left fails with `HoldemErrorCode.InvalidSeat`, so a second kick fails too.

```csharp
// Kick Bob in the middle of a hand: he folds now and is gone for the next one.
var kicked = await engine.ApplyAsync(HoldemAction.KickSeat(gameId, 2), ct);
```

See [`HoldemActionKickSeat`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.md).

## Blind schedules

Without a schedule every hand is dealt at `InitialSmallBlind` and the big blind is twice that. Set `GameOptionData.BlindSchedule` to a [`BlindScheduleData`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.Dto.BlindScheduleData.md) to let the blinds rise by levels. The engine has no clock, so every trigger counts hands or players, and the game stays deterministic and replayable.

### Triggers

A schedule needs at least one of three triggers:

| Trigger | Fires when |
|---|---|
| `IncreaseEveryHands` | the hand being dealt is that many hands after the one that started the current level |
| `PlayerThresholds` | the number of players dealt into the hand is at or below a threshold that has not fired yet; each value fires once, and values lie in `2..MaxPlayersCount - 1` |
| `IncreaseOnElimination` | fewer players are dealt into the hand than into the previous one: a bust, a leave or a kick |

The blinds change only when a hand is dealt, and a hand's blinds never change after the deal. However many triggers fire for one hand, the level rises by **at most one**. The first hand is level 0, and any threshold it already satisfies counts as passed without raising. A player joining never fires a trigger, and levels never fall.

### Two modes

`BlindScheduleData.Mode` says how the next level's small blind is worked out:

- `BlindIncreaseMode.Multiplier` (the default): the new small blind is the previous one times `Multiplier` (above 1, at most two decimals, 2 by default), rounded to the cent away from zero and capped at `MaxSmallBlind` when that is set.
- `BlindIncreaseMode.Levels`: `SmallBlindLevels` lists the small blinds after the initial one, strictly increasing; once the list is exhausted the last one holds. `MaxSmallBlind` is not allowed in this mode.

An invalid schedule fails `CreateGame` with `HoldemErrorCode.InvalidGameOptions`.

### Reading the blinds

Each hand carries its own blinds: `HandData.SmallBlind`, `HandData.BigBlind` and `HandData.BlindLevel` (0 is the initial level). Read them from the hand rather than deriving them from the options.

### A worked example

Initial small blind 1, a new level every 10 hands, multiplier 2, capped at 8:

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    InitialSmallBlind = 1,
    BlindSchedule = new BlindScheduleData
    {
        IncreaseEveryHands = 10,
        Multiplier = 2,
        MaxSmallBlind = 8,
    },
}), ct);
```

| Hands | Level | Small blind / big blind |
|---|---|---|
| 1–10 | 0 | 1 / 2 |
| 11–20 | 1 | 2 / 4 |
| 21–30 | 2 | 4 / 8 |
| 31–40 | 3 | 8 / 16 |
| 41 onward | 4 and up | 8 / 16 (the cap holds while the level number keeps counting) |

The same schedule in `Levels` mode would be `Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [2, 4, 8]`, with no `Multiplier` or `MaxSmallBlind`.

## Antes

An ante is a forced payment made before the cards are played, on top of the blinds. Set `GameOptionData.Ante` to a positive amount with at most two decimal places (`null`, the default, means no ante) and `GameOptionData.AnteType` to say who posts it. Without an ante a game plays as in 1.3.

| `AnteType` | Who posts the ante | When |
|---|---|---|
| `Classic` (the default) | every seat dealt into the hand | at the deal, before the blinds |
| `BigBlind` | the big blind seat alone, heads-up included | right after its big blind |

An ante is **dead money**. It goes into the pot and into the seat's total contribution, so it counts in the side pots, but it is never part of a street wager: it does not raise the amount to call, it does not move the minimum raise, and the big blind's option is unchanged. Under Pot-Limit it is part of the pot a raise is measured against, see [Legal actions and bet sizing](evaluations.md).

### Short stacks

The button and the blind seats are fixed by position before any chip is posted, whatever the seats can cover.

- **Classic**: every seat posts `min(ante, stack)` before the blinds. A blind seat that its ante leaves all in keeps its place and posts a blind of `0`. A small blind that holds the standing wager because the big blind posted no more has an option, as the big blind would.
- **Big blind ante**: the big blind seat posts its blind first, then `min(ante, stack left)`. Nobody else posts an ante.
- **Side pots**: a classic ante counts in the seat's level, so a seat short on its ante wins only what it matched of the others. A big blind ante sets no level and is dead money in the lowest pot, so a big blind all in with its ante on top of a called wager loses the ante to the caller. That is the TDA treatment of the big blind ante; other rooms rule differently.

### Reading the ante

`HandData.Ante` is the hand's ante, or `0` when the hand has none. Under a blind schedule it follows the small blind: the engine works it out once, when the hand is dealt, as `round(Ante × SmallBlind / InitialSmallBlind, 2)` (rounded to the cent away from zero). Each ante is also a row of the [hand ledger](hand-ledger.md), with `ActionType` `Ante`.

`HandData.Pot` includes the antes from the deal on, and `HandData.StreetPot` and `SeatData.BetSize` never do, so `Pot + StreetPot` is always the whole pot.

### A worked example

Six seats of 100 at blinds 1/2, the button on seat 6 and a classic ante of 1:

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    InitialSmallBlind = 1,
    Ante = 1,
    AnteType = AnteType.Classic,
}), ct);
```

After the deal:

| What | Value |
|---|---|
| Ledger | six `Ante` rows of 1 (seats 1 to 6), then the small blind of 1 (seat 1) and the big blind of 2 (seat 2) |
| `HandData.Pot` | 6, the six antes |
| `HandData.StreetPot` | 3, the two blinds |
| Seat 3 to call | 2, as without an ante |
| Seat 3's minimum raise | to 4, as without an ante |
| Seat 3's stack | 99 |

If seats 3 to 6 and the small blind fold, the big blind wins the 9 in the pot. Its net is +6: the 9 it wins less the 3 it put in itself, its ante of 1 and its blind of 2. The small blind nets −2 and each of seats 3 to 6 nets −1.

Under Pot-Limit the same deal lets seat 3 raise to at most 13, where the same game without the ante allows 7: the standing wager of 2 plus the pot once it has called, 6 antes, 3 of blinds and its call of 2.

With `AnteType.BigBlind` and an ante of 2 the ledger is the small blind of 1, the big blind of 2 and an `Ante` row of 2 for seat 2, which then has 96. `Pot` is 2 and `StreetPot` 3. The big blind still has its option when everyone else calls, and a raise is still to at least 4.

Under the blind schedule `IncreaseEveryHands = 1, Multiplier = 2` and a classic ante of 1 at blinds 1/2, hand 1 has an ante of 1, and hand 2, at blinds 2/4, an ante of 2.

Tournaments are still to come, see the [roadmap](../roadmap.md).
