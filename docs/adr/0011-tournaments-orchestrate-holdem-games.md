# ADR-0011: Tournaments orchestrate Hold'em games

- **Status:** Accepted
- **Date:** 2026-11-11
- **Issue:** [#710](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/710), milestone
  `1.4.0 - holdem-tournaments`

## Context

Epic [#342](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/342) (the roadmap's
"Tournaments: antes and blind levels, then SNG, then MTT") ships as one release, `1.4.0`. The
roadmap leaves the shape open: "a tournament aggregate, likely its own package". The maintainer
decided it before planning, and the issues of the milestone implement the decisions below. This
record is where the next reader finds them.

Measured on `main` at `c97e6ba`: there is no tournament code, `HandActionType` has no ante, and
`PhhReplayRunner.IsSupported` rejects every hand with a non-zero ante.

## Decision

### 1. Antes are a Hold'em game option, for cash games and tournaments alike

- `HandActionType.Ante` is a new member.
- `AnteType` has two values: `Classic = 1` (every seat dealt in posts the ante) and `BigBlind = 2`
  (the big blind seat alone posts it).
- `GameOptionData.Ante` is a `decimal?` (`null` means no ante) and `GameOptionData.AnteType`
  defaults to `Classic`.
- `HandData.Ante` is the hand's ante.
- Antes are dead money: they go into the pot and count towards a seat's total contribution (side
  pots), never towards its street wager.
- A classic ante is posted before the blinds. A big blind ante is posted after the big blind, from
  what is left: the big blind takes priority, as in the TDA rules.
- The button and the blinds go by position, settled before any ante is collected: a blind seat its
  classic ante empties keeps its place and posts a blind of nothing, and a seat too short for its
  blind posts what it has. Tournament short stacks make this the common case, and the synthetic
  ante corpus (`pokerkit-antes.phhs.gz`) is the oracle for it (#744).
- Under a blind schedule the ante keeps its ratio to the small blind:
  `round(Ante × SmallBlind / InitialSmallBlind, 2)`.

### 2. A tournament is an orchestrator of ordinary Hold'em games

Each tournament table is a `Game` of the existing engine. The tournament adds its own tables to the
database (`Tournament`, `TournamentEntry`, `TournamentTable`, in the `tce_holdem` schema, or with
the `tce_holdem_` prefix on SQLite) and changes no existing table except through additive columns.
It lives in the core package, in the namespace `TabletopCardEngine.Holdem.Tournaments` (the
placement [ADR-0008](0008-harmonize-the-public-api.md) requires), uses the same `HoldemDbContext`,
and is saved in the same transaction as the table it touches. There is no new package.

### 3. One public entry point

`IHoldemTournament.ApplyAsync(TournamentAction, CancellationToken)` returns
`HoldemResult<TournamentData>` and is registered by `AddHoldem`. The actions are `CreateTournament`,
`GetTournament`, `Register`, `Unregister`, `StartTournament` and `DealTable`. Players act on their
table through `IHoldemTable` with the table's game id (`Play`, `GetGame`). Every other Hold'em
action on a tournament table fails with `ManagedByTournament`.

### 4. Freezeout only

Registration closes at `StartTournament`. There are no rebuys, add-ons, late registration or
bounties.

### 5. Levels run on a clock

A tournament has a list of levels (small blind, ante, duration); the big blind is twice the small
blind. The level is a function of the time since `StartTournament`, read from a `TimeProvider`
(`HoldemOptionsBuilder.WithTimeProvider`, `TimeProvider.System` by default), and is fixed for a hand
when the hand is dealt. The last level lasts forever.

### 6. The host drives the tournament between hands with `DealTable`

After a table's hand ends, `DealTable(tableNo)` records that table's eliminations, finishes the
tournament when one player is left, breaks or balances tables, seats players moved to this table,
and deals the next hand at the current level. It touches the tournament and that one table's game
only, so tables never block each other's play. When two `DealTable` calls race on the same
tournament, the loser gets `Conflict` and retries.

### 7. Places and prizes

Players busted in the same hand at the same table are placed by their stack at the start of that
hand (bigger stack, better place); equal start stacks tie and share the prizes of the tied places
equally. Across tables, the order is the order in which `DealTable` recorded the busts. The prize
pool is `BuyIn × entries` (fees are the host's business). `Payouts` are percentages per place,
rounded to the cent, the remainder going to first place.

### 8. Seating, balancing and breaking

`StartTournament` draws seats at random (`SeatDrawSeed` makes the draw reproducible) into
`ceil(entries / MaxPlayersPerTable)` tables whose sizes differ by at most one. At `DealTable(A)`:

- when the players left fit into one table fewer, table A breaks and its players move to the tables
  with the fewest players;
- otherwise, when A has at least two players more than the shortest table, A sends the player due to
  post the next big blind to the shortest table, until the difference is at most one.

A moved player carries the stack and is seated at the target table's next `DealTable`.

### 9. Out of scope for 1.4.0

Rebuys, add-ons, late registration, bounties, breaks, hand-for-hand play, deal-making,
disqualification and satellites.

## Consequences

- **The public surface grows in the core package.** `IHoldemTournament`, `TournamentAction` and its
  records, `TournamentData`, the new `AnteType`, `HandActionType.Ante`, `GameOptionData.Ante` and
  `AnteType`, `HandData.Ante` and `WithTimeProvider` are additions, so a minor version under
  [docs/versioning.md](../versioning.md), and the API baseline and the generated reference follow.
- **The schema gains three tables and additive columns,** so every provider package needs a
  migration, with an upgrade fixture, under [docs/persistence.md](../persistence.md).
- **Cash games gain antes too.** The ledger, the pot maths, the PHH replay and the demo must handle
  an `Ante` row; `PhhReplayRunner.IsSupported` stops rejecting hands with an ante once they do.
- **The host owns the clock between hands.** Nothing deals the next hand by itself: a host that
  never calls `DealTable` stalls its tournament, and a level change takes effect at the next deal,
  not mid-hand.
- **Tables stay independent,** so players at different tables never wait for each other, at the price
  that the order of busts across tables is the order of `DealTable` calls rather than of the hands.
- **Open points,** not decided here: who may call each tournament action is the host's business, as
  with `KickSeat`; what the observability signals of a tournament are; and how the demo presents a
  tournament.
