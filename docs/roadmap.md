# TabletopCardEngine roadmap

## What this document is

The features planned after v1.0, their order, and what the v1.0 API already does for them. Implementation is tracked in the [`v1.x · Features` milestone](https://github.com/DigitalAbsurd/TabletopCardEngine/milestone/14). Planning history lives in GitHub, not here.

## Versioning of the roadmap

Every feature below is planned as a **minor** 1.x release: each one adds members, options, enum members or migrations and removes nothing, which the SemVer policy ([docs/versioning.md](versioning.md)) counts as minor; "v2" in labels and milestone names means "planned after 1.0", and a 2.0 happens only for a break. The v1.0 public API is frozen (#327).

## Scope

Five features are in:

- Pot-Limit and Fixed-Limit (✓ **Shipped in 1.3.0**)
- Tournaments (SNG/MTT, blind levels, antes) (✓ **Shipped in 1.4.0**)
- Rebuy, buy-in and kick-off
- 9 to 10 seats (✓ **Shipped in 1.1.0**)
- Hand history export (PHH)

Out of scope for this roadmap (candidates considered and not selected):

- Omaha
- Rake
- Timeouts and a time bank
- Straddle
- Run-it-twice
- An event stream for real-time clients (SignalR)
- Multi-tenancy

## Licensing

All five are open source under Apache-2.0 (ADR-0001), in this repository; none is a commercial add-on. How the project is funded is decided in the `v1.0 · Legal & dependency audit` milestone and does not change this.

## Priorities

| Order | Feature | Value | Cost | Risk | Why this order |
|---|---|---|---|---|---|
| 1 | 9 to 10 seats | High | Low | Low | The engine is ring-generic; only validators and hosts cap at 6. Opens full-ring play cheaply. |
| 2 | Hand history export (PHH) | Medium | Low | Low | Built on the v1.0 ledger (#325); the vendored PHH corpora give a round-trip oracle. |
| 3 | Rebuy, buy-in and kick-off | High | Medium | Medium | Real cash games need seats to be vacated and stacks topped up; touches seat lifecycle and rehydration. |
| 4 | Pot-Limit and Fixed-Limit (✓ Shipped in 1.3.0) | Medium | Medium | High | New betting rules; wrong sizing is a silent rules bug. Needs its own oracle. |
| 5 | Tournaments: antes and blind levels, then SNG, then MTT (✓ Shipped in 1.4.0) | High | High | High | The largest feature; split into three steps, shipped together. Builds on 1, 3 and the per-hand blinds of #323. |

## 9 to 10 seats

[#338](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/338)

**Shipped in 1.1.0:**

The engine now supports No-Limit Hold'em in three table formats chosen at game creation: Heads-Up (2-Max, exactly 2 players), 6-Max (2 to 6 players, the default), and Full Ring (7 to 10 players). See [the 1.1.0 milestone](https://github.com/DigitalAbsurd/TabletopCardEngine/milestone/17) for the implementation.

Also shipped in 1.1.0: seat lifecycle management — players can join empty seats between hands with `StartHand.JoinSeats`, and stand up to leave with `LeaveSeat`, freeing the seat for a new player. When fewer than two seats have money left, new players must join before the next hand can start (`NotEnoughPlayers`).

## Hand history export (PHH)

[#339](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/339)

**Already in v1.0:**

- `HandData.Actions` with `SeatNo` and `Amount` on every `PlayerActionData` (#324, #325)
- Every seat's hole cards and the board

**Comes with the feature:**

- A public exporter in the library (or a package of its own)
- A way to read a past hand (today `GameData` carries the current hand only)
- Replacing the demo's `Phh/PhhWriter.cs`, which guesses seats from its own call journal, writes broken `players` quoting, throws on `Bet` and `AllIn`, and puts the whole board in one `d db` line

**Oracle:**

- Replay a PHH hand, export it, compare with the source

## Rebuy, buy-in and kick-off

[#340](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/340)

**Already in v1.0:**

- `PlayerJoinData.BuyInAmount`
- The `BuyIn` table (one row per seat per hand)
- `Player.HandNoEnd` (never set yet)
- The top-up branch in `HandDealer.CalculateStartStacks` (unreachable until `SeatValidation` allows an occupied seat)

**Shipped in 1.1.0:**

- `LeaveSeat` action to stand a player up between hands, freeing the seat
- `StartHand.JoinSeats` to seat new players into empty seats

**Shipped in 1.2.0:**

- `TopUp` action for both top-up and rebuy: which one it is follows from the seat's stack, and the chips go in at the next hand
- `KickSeat` action: between hands it is `LeaveSeat`, during a hand an immediate fold
- `GameOptionData.AllowTopUp`, `AllowRebuy` and `MaxRebuys`, with `SeatData.PendingBuyIn` and `SeatData.Rebuys`
- The buy-in window, `MinBuyIn` and `MaxBuyIn`, enforced on joins, top-ups and rebuys

See [Buy-ins, top-ups and blind schedules](concepts/buy-ins-and-blinds.md).

**Oracle:**

- Invariants as in `MultiHandSessionTests` (chip conservation including buy-ins)

## Pot-Limit and Fixed-Limit

[#341](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/341)

**Already in v1.0:**

- `GameOptionData.BettingStructure`
- The supported-variants table (#321) so a new enum member is not playable until listed
- `HandData.Evaluations` whose `MinValue`/`MaxValue` carry a pot-limit cap or a fixed size as they are

**Shipped in 1.3.0:**

- `BettingStructure.PotLimit` and `BettingStructure.FixedLimit`, with the `Raise` window computed per structure and `AllIn` resolved against it
- `GameOptionData.RaiseCap` for Fixed-Limit (one migration per provider)
- Two No-Limit rule fixes: the minimum raise, and no reopening of the betting after an incomplete all-in raise (TDA Rule 47)

See [Betting structures](concepts/betting-structures.md).

**Oracle:**

- PHH hands of the fixed-limit variant (WSOP and ACPC), and synthetic pot-limit and fixed-limit corpora played by pokerkit
- The hand-built pot-limit and fixed-limit suites

## Tournaments: antes and blind levels, then SNG, then MTT

[#342](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/342)

**Already in v1.0:**

- `HandData.SmallBlind`/`BigBlind` (#323), so clients read the hand's blinds instead of deriving them from `GameOptionData`

**Shipped in 1.2.0:** blind levels for cash games: `GameOptionData.BlindSchedule` (`BlindScheduleData`) and the per-hand blinds on `Hand`, read as `HandData.SmallBlind`, `BigBlind` and `BlindLevel`.

**Shipped in 1.4.0** (milestone [`1.4.0 - holdem-tournaments`](https://github.com/DigitalAbsurd/TabletopCardEngine/milestone/22)), in three steps released together:

1. *Antes* — `GameOptionData.Ante` and `AnteType` (`Classic`, `BigBlind`), `HandActionType.Ante` and `HandData.Ante`, antes posted as dead money in the ledger and the side pots, for cash games and tournaments alike. See [Buy-ins, blinds and antes](concepts/buy-ins-and-blinds.md).

2. *Sit-and-go* — `IHoldemTournament` with `CreateTournament`, `GetTournament`, `Register`, `Unregister`, `StartTournament` and `DealTable`: blind levels on a clock (`HoldemOptionsBuilder.WithTimeProvider`), eliminations, places, ties and prizes. Each table is an ordinary game played through `IHoldemTable`, which refuses everything but `Play` and `GetGame` on it (`ManagedByTournament`).

3. *Multi-table tournaments* — seating over several tables, and breaking and balancing them at `DealTable`. No new package: the tournament lives in the core, in `TabletopCardEngine.Holdem.Tournaments` ([ADR-0011](adr/0011-tournaments-orchestrate-holdem-games.md)). The ASP.NET Core package maps the routes (see [Tournaments in the ASP.NET Core guide](aspnetcore.md#tournaments)) and the demo plays a tournament table by table.

See [Tournaments](concepts/tournaments.md). Freezeout only: rebuys, add-ons, late registration, bounties, breaks, hand-for-hand play, deal-making, disqualification and satellites are out of scope.

**Oracle:**

- PHH hands with antes: the WSOP 2023 no-limit big blind ante hands and the synthetic pokerkit ante corpora (classic and big blind), replayed exactly
- The hand-built ante suite and the multi-hand session oracle with antes
- The tournament session oracle: whole sit-and-gos and multi-table tournaments played to the end, with chip conservation, balanced tables, levels and prizes as the invariants

## Extension points already in v1.0

- #321: supported variants table
- #322: `IHoldemTable` and `GameOptionData` docs
- #323: per-hand blinds
- #324: `HandData.Actions` with `SeatNo`
- #325: `HandData.Actions` with `Amount`
- #326: public API freeze
- #327: API baseline
- Games register on `TabletopCardEngineBuilder`, each with its own namespace, `DbContext` and schema (ADR-0008). Omaha stays out of scope.
