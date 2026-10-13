# TabletopCardEngine roadmap

## What this document is

The features planned after v1.0, their order, and what the v1.0 API already does for them. Implementation is tracked in the [`v1.x · Features` milestone](https://github.com/DigitalAbsurd/TabletopCardEngine/milestone/14). Planning history lives in GitHub, not here.

## Versioning of the roadmap

Every feature below is planned as a **minor** 1.x release: each one adds members, options, enum members or migrations and removes nothing, which the SemVer policy ([docs/versioning.md](versioning.md)) counts as minor; "v2" in labels and milestone names means "planned after 1.0", and a 2.0 happens only for a break. The v1.0 public API is frozen (#327).

## Scope

Five features are in:

- Pot-Limit and Fixed-Limit
- Tournaments (SNG/MTT, blind levels, antes)
- Rebuy, buy-in and kick-off
- 9 to 10 seats
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
| 4 | Pot-Limit and Fixed-Limit | Medium | Medium | High | New betting rules; wrong sizing is a silent rules bug. Needs its own oracle. |
| 5 | Tournaments: antes and blind levels, then SNG, then MTT | High | High | High | The largest feature; split into three steps, each its own release. Builds on 1, 3 and the per-hand blinds of #323. |

## 9 to 10 seats

[#338](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/338)

**Already in v1.0:**

- `SeatNo` is a `byte`
- `HandTable` holds occupied seats only and walks them as a ring
- The database has no seat constraint

**Comes with the feature:**

- Lift the cap in `HoldemOptionsBuilder.WithSettings` (throws above 6)
- `HoldemSettings.GamePlayersMaxCount` docs
- `[Range(2, 6)]` in `TabletopCardEngine.AspNetCore/HoldemHostOptions.cs`
- The demo's `DemoGameDefaults.MaxPlayers`
- The 2–6 dropdown in `GameSetup.razor`
- The demo's PHH replay then accepts 7+ player hands

**Oracle:**

- `MultiHandSessionTests` with a `[DataRow(10)]`
- A full-ring PHH fixture if one can be sourced under a permissive license

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

**Comes with the feature:**

- New `HoldemAction` records (leave or kick a seat, top up)
- Relaxed `SeatValidation`
- `HandTableFactory` honouring `HandNoEnd`
- Removed from v1.0 in #140 on purpose: `BuyIns`, `KickOffSeats`, `AllowRebuy`

**Oracle:**

- Invariants as in `MultiHandSessionTests` (chip conservation including buy-ins)

## Pot-Limit and Fixed-Limit

[#341](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/341)

**Already in v1.0:**

- `GameOptionData.BettingStructure`
- The supported-variants table (#321) so a new enum member is not playable until listed
- `HandData.Evaluations` whose `MinValue`/`MaxValue` carry a pot-limit cap or a fixed size as they are

**Comes with the feature:**

- New `BettingStructure` members
- `BettingRules.MinRaiseAmount`/`LegalActions`/`Evaluate` per structure (the No-Limit cap is `Stack + StreetWager`, and `Evaluate` always appends an all-in raise)
- `AllIn` resolution in `PlayActionHandler`
- Fixed-Limit bet sizes, a raise cap and incomplete all-in raises
- New `GameOptionData` properties and `Game` columns (one migration per provider)

**Oracle:**

- PHH hands of the fixed-limit variant
- A hand-built pot-limit suite

## Tournaments: antes and blind levels, then SNG, then MTT

[#342](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/342)

**Already in v1.0:**

- `HandData.SmallBlind`/`BigBlind` (#323), so clients read the hand's blinds instead of deriving them from `GameOptionData`

**Comes with the feature:**

Three steps:

1. *Antes and blind levels* — `HandActionType.Ante`, per-hand blind and ante values on `Hand`, `PostBlinds`/`ExcludeShortOfMoneySeats` posting antes, the ledger and pot maths treating antes as dead money

2. *SNG* — elimination order and payouts on top of a game

3. *MTT* — several tables, balancing and a tournament aggregate, likely its own package

**Oracle:**

- PHH hands with antes, which `PhhSupport.IsSupported` rejects today

## Extension points already in v1.0

- #321: supported variants table
- #322: `IHoldemTable` and `GameOptionData` docs
- #323: per-hand blinds
- #324: `HandData.Actions` with `SeatNo`
- #325: `HandData.Actions` with `Amount`
- #326: public API freeze
- #327: API baseline
