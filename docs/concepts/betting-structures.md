# Betting structures

A betting structure decides how much a player may bet or raise. The engine plays three of them in Texas Hold'em cash games: No-Limit, Pot-Limit and Fixed-Limit. Everything else about the game (seats, blinds, side pots, showdown) is the same in all three, and so is the way you play: read `HandData.Evaluations`, then send one of the actions it offers. See [Legal actions and bet sizing](evaluations.md) for how to read the windows.

## Choosing one

`GameOptionData.BettingStructure` ([`BettingStructure`](../api/TabletopCardEngine/TabletopCardEngine.Holdem.BettingStructure.md)) picks the structure when the game is created, and it cannot change afterwards:

| Value | A bet or raise may be |
|---|---|
| `NoLimit` (the default) | Anything from the minimum up to the whole stack |
| `PotLimit` | Anything from the minimum up to the size of the pot after a call |
| `FixedLimit` | Exactly one fixed size |

An action the structure does not allow, or an amount outside its window, fails with `HoldemErrorCode.IllegalAction` and saves nothing.

## The minimum raise

In No-Limit and Pot-Limit a raise must be at least the standing wager plus the last full raise on the street, and never less than one big blind. Pre-flop the first raise must therefore bring the wager to two big blinds. Fixed-Limit has no range: its size is fixed (see below).

`Raise` evaluations are the street total to raise *to*, not the increment. At blinds 1/2 the first player to act pre-flop sees a `Raise` window starting at 4.

## Incomplete all-in raises

An all-in raise that lifts the standing wager by less than a full raise is *incomplete*. It still has to be called, but it does not reopen the betting to a player who has already acted on the street (TDA Rule 47):

- In No-Limit and Pot-Limit the betting is reopened once the wager has risen by a full raise since that player's last voluntary action. Raises are counted cumulatively, so two short all-ins can add up to one full raise.
- In Fixed-Limit the threshold is half a bet: an all-in raise of at least half a bet reopens the betting, and a smaller one does not (nor does it count towards the raise cap).

A blind post is not a voluntary action, so the big blind keeps its option.

For a player who may not raise, `Evaluations` offers `Fold` and `Call` only, with no `Raise`. A `Raise`, or an `AllIn` that would be a raise, fails with `IllegalAction`.

## Pot-Limit

The largest raise is the standing wager plus the pot as it will be once the player has called:

```
MaxValue = StandingWager + (every chip committed this hand + MoreToCall)   // then capped at the stack
```

Every chip means the blinds and the wagers of players who have already folded too. At blinds 1/2 with three seats of 200, the first player to act may raise to `2 + (3 + 2) = 7`, so the window is 4 to 7. After that raise, the small blind may raise to `7 + (10 + 6) = 23`, a window of 12 to 23.

An `AllIn` above the pot limit is illegal. The all-in raise is offered on its own only when the stack cannot reach the minimum raise.

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    BettingStructure = BettingStructure.PotLimit,
    MaxPlayersCount = 3,
    InitialSmallBlind = 1,
    InitialDealerSeatNo = 3,
}), ct);
var gameId = created.Value!.GameId;

var started = await engine.ApplyAsync(HoldemAction.StartGame(gameId, new[]
{
    new PlayerJoinData { SeatNo = 1, Name = "Alice", BuyInAmount = 200 },
    new PlayerJoinData { SeatNo = 2, Name = "Bob", BuyInAmount = 200 },
    new PlayerJoinData { SeatNo = 3, Name = "Carol", BuyInAmount = 200 },
}), ct);

var raise = started.Value!.HandData!.Evaluations
    .Single(e => e.PlayerActionType == PlayerActionType.Raise);
// raise.MinValue == 4, raise.MaxValue == 7
```

## Fixed-Limit

Bets and raises have one size, taken from the blinds of the hand, so a [blind schedule](buy-ins-and-blinds.md) raises the bets with the blinds. The **small bet** is the big blind, used pre-flop and on the flop. The **big bet** is twice that, used on the turn and the river. The `Raise` evaluation has a single size, so `MinValue == MaxValue`: the standing wager plus one bet. When the stack falls short of it, the window is the all-in total.

`GameOptionData.RaiseCap` is the most bets a street may hold. It defaults to 4, a bet and three raises, and pre-flop the big blind counts as the first bet. `null` means no cap. Once the street is at the cap there is no `Raise` entry, and an `AllIn` beyond the window fails with `IllegalAction`. `RaiseCap` must be at least 1 when set, and `NoLimit` and `PotLimit` ignore it.

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    BettingStructure = BettingStructure.FixedLimit,
    MaxPlayersCount = 2,
    InitialSmallBlind = 1,
    RaiseCap = 4,
}), ct);
var gameId = created.Value!.GameId;

var started = await engine.ApplyAsync(HoldemAction.StartGame(gameId, new[]
{
    new PlayerJoinData { SeatNo = 1, Name = "Alice", BuyInAmount = 200 },
    new PlayerJoinData { SeatNo = 2, Name = "Bob", BuyInAmount = 200 },
}), ct);

// Blinds 1/2: the player to act may raise to 4 and to nothing else.
var raise = started.Value!.HandData!.Evaluations
    .Single(e => e.PlayerActionType == PlayerActionType.Raise);
// raise.MinValue == 4, raise.MaxValue == 4
```

## No-Limit

No-Limit is the default and needs no option. The `Raise` window runs from the minimum to the stack, and an `AllIn` is legal wherever raising is.

```csharp
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    BettingStructure = BettingStructure.NoLimit,
    MaxPlayersCount = 3,
    InitialSmallBlind = 1,
    InitialDealerSeatNo = 3,
}), ct);
// StartGame as above: with stacks of 200 the first window is 4 to 200.
```

## How it is tested

The rules are checked against recorded and independent play. The PHH corpora hold real fixed-limit hands (the 2023 WSOP Poker Players Championship and the 2014 Annual Computer Poker Competition) with their results, and synthetic pot-limit and fixed-limit hands played by pokerkit, an independent implementation of the rules, covering all-ins, side pots and incomplete raises. Hand-built suites pin the windows of each structure and the reopening rules. The corpora and their provenance are described in the [fixture README](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/tests/TabletopCardEngine.Tests/Resources/phh/README.md).
