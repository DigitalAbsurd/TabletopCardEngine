# PHH test fixtures

Vendored corpora, all but the last from the [PHH dataset](https://github.com/uoftcprg/phh-dataset) at commit
`e47fbd5816372360bade4de5d712346fe1bb70f6` (MIT — see
[LICENSE-phh-dataset.txt](LICENSE-phh-dataset.txt)); `acpc-fixedlimit.phhs.gz` is from the Zenodo edition, under CC-BY-4.0.
The three `pokerkit-*` files are not vendored: this project generates them with pokerkit (see below).

| File | Hands | What it is |
| --- | --- | --- |
| `pluribus.phhs.gz` | 10 000, all replayable | Bot play, six-handed. Exact oracle. |
| `handhq-headsup.phhs.gz` | 2 351, of which 2 145 replayable | Real online play, heads-up. Structural oracle. |
| `handhq-fullring.phhs.gz` | 1 505, of which 1 505 replayable | Real online play, full-ring (7–10 seated). Structural oracle. |
| `wsop-fixedlimit.phhs.gz` | 7, all replayable | Real tournament play, fixed-limit, five-handed. Exact oracle. |
| `wsop-nolimit.phhs.gz` | 11, all replayable | Real tournament play, no-limit, five-handed, big blind ante. Exact oracle. |
| `acpc-fixedlimit.phhs.gz` | 6 000, all replayable | Bot play, fixed-limit, heads-up and three-player. Exact oracle through `_results`. From the Zenodo edition, CC-BY-4.0. |
| `pokerkit-potlimit.phhs.gz` | 2 000, synthetic, 739 tagged | pokerkit play, pot-limit, 2 to 10 seats, all-ins and side pots. Exact oracle outside the tagged hands. |
| `pokerkit-fixedlimit.phhs.gz` | 2 000, synthetic, 190 tagged | pokerkit play, fixed-limit, 2 to 10 seats, all-ins and side pots. Exact oracle outside the tagged hands. |
| `pokerkit-antes.phhs.gz` | 2 000, synthetic, 845 tagged | pokerkit play, no-limit, 2 to 10 seats, classic antes (1 000 hands) and big blind antes (1 000 hands), all-ins on the ante and side pots. Exact oracle outside the tagged hands. |

## `pluribus.phhs.gz`

A vendored copy of the **`data/pluribus`** subset of the
[PHH dataset](https://github.com/uoftcprg/phh-dataset), packed into a single gzipped
[`.phhs`](https://pokerkit.readthedocs.io/en/stable/notation.html) file (TOML array of tables with
numeric headers `[1]`, `[2]`, … — the layout `pokerkit`'s `HandHistory.dump_all` emits, so the
fixture stays readable by third-party PHH tooling). Each table is preceded by a comment naming the
upstream file it came from.

- **Contents:** 10 000 hands (5.9 MB uncompressed), all 6-handed no-limit Texas hold'em,
  blinds 50/100, no antes, every stack starting at 10 000

### Why this subset

These are all 10 000 hands played by the **Pluribus** bot, published in the supplementary material
of Brown & Sandholm (2019). They are uniquely well suited as an engine oracle:

- every player's hole cards are recorded (all six seats were bots), so a hand can be replayed
  deterministically through `ICardShuffler` with nothing left to guess;
- `finishing_stacks` records the exact result, which gives the showdown and pot-splitting logic a
  ground truth to compare against;
- all amounts are whole chips, which matters because NPokerEngine holds amounts as `float`;
- blinds are a plain 50/100 with no antes, matching what the engine supports
  (`StartHandActionHandler` derives the big blind as `InitialSmallBlind * 2`).

Stacks are reset to 10 000 before every hand, so each hand is replayed as its own `Game`.

## `handhq-headsup.phhs.gz`

The heads-up hands of the first five files of **`data/handhq/PS-2009-07-01_2009-07-23_50NLH_OBFU/0.5`**
— anonymised PokerStars \$0.25/\$0.50 no-limit hold'em logs scraped in July 2009. Only hands whose
`blinds_or_straddles` has exactly two entries are packed; that is a mechanical "is this heads-up"
test, and every other requirement is decided at test time by `PhhReplayRunner.IsSupported`, so the
filter has one home. Records are copied verbatim apart from the renumbered `[N]` header.

The `pluribus` subset is entirely six-handed, and it is the only subset of the dataset that is both
no-limit hold'em and an exact oracle. Heads-up needed its own corpus:

- the WSOP subset's 11 no-limit hold'em hands are all five-handed (they are `wsop-nolimit.phhs.gz` below, an exact
  oracle for the big blind ante);
- the four historical selections are pot-limit Omaha, 2-7 triple draw, short-deck and a hand with
  antes;
- the 278 million heads-up no-limit hands of the Annual Computer Poker Competition are excluded from
  the GitHub repository (they are on Zenodo only).

**What this corpus does and does not verify.** Positions, start stacks, hole cards, the seat on the
clock at every decision, street progression and every board card are all checked against the history,
same as for Pluribus. The money is not: handhq records no `finishing_stacks`, and its pots are raked,
which this engine has no concept of — so an exact stack comparison is impossible in principle, not
merely unavailable. `Sum(Net) == 0` is still asserted, as an engine invariant rather than against the
history.

A hand is filtered out when it reaches a showdown the history did not write every card of, which is
normal for real play: only what was shown is recorded. 206 of the 2 351 are dropped that way.

### Seating heads-up

The specification's two rules — `p1` posts the small blind, the last player has the button — cannot
both hold with two players. These histories settle it by their action order: the **last** player acts
first pre-flop and `p1` first on every street after it, which is the button posting the small blind.
So heads-up, `p1` is the big blind. The engine reaches the same layout from
`InitialDealerSeatNo = PlayerCount` unchanged, because it moves the small blind onto the button itself
when only two seats are funded.

## `handhq-fullring.phhs.gz`

The full-ring hands (7 to 10 seated) from two sources of the **`data/handhq`** subset of the
[PHH dataset](https://github.com/uoftcprg/phh-dataset):
- 7–9 seated hands from the first five files of **`PS-2009-07-01_2009-07-23_50NLH_OBFU/0.5`** — anonymised PokerStars \$0.25/\$0.50 no-limit hold'em logs scraped in July 2009
- 10 seated hands from the first twenty files of **`PTY-2009-07-01_2009-07-23_400NLH_OBFU/4`** — anonymised PartyPoker \$2/\$4 no-limit hold'em logs from July 2009

Only hands whose `blinds_or_straddles` has 7–9 entries (for 7–9 players) or exactly 10 entries (for 10 players) are
kept; that is a mechanical "is this full-ring" test, and every other requirement is decided at test time by
`PhhReplayRunner.IsSupported`, so the filter has one home. Records are copied verbatim apart from the renumbered
`[N]` header.

| Player count | Hands |
| --- | --- |
| 7 players | 178 |
| 8 players | 524 |
| 9 players | 568 |
| 10 players | 235 |
| **Total** | **1 505** |

**What this corpus does and does not verify.** Positions, start stacks, hole cards (where shown), the seat on the
clock at every decision, street progression and every board card are all checked against the history, same as for
heads-up. The money is not: handhq records no `finishing_stacks`, and its pots are raked, which this engine has no
concept of — so an exact stack comparison is impossible in principle, not merely unavailable. `Sum(Net) == 0` is still
asserted, as an engine invariant rather than against the history.

A hand is filtered out when it reaches a showdown the history did not write every card of, which is normal for real
play: only what was shown is recorded.

### Seating full-ring

The PHH specification says `p1` posts the small blind and the last player has the button. In full-ring the first player
is not the last, so this layout always holds: `p1` is the small blind, `p2` is the big blind, and players 3–*n* act
in order down to the dealer button (the last player). The engine reaches the same layout from `InitialDealerSeatNo = PlayerCount`
directly.

## `wsop-fixedlimit.phhs.gz`

The seven hands with `variant = 'FT'` of **`data/wsop/2023/43/5`**, the final table (day 5) of the 2023 WSOP
\$50,000 Poker Players Championship: `01-39-18`, `01-42-31`, `01-44-49`, `01-45-43`, `01-46-42`, `01-47-38` and
`01-51-27`, ordered by file name and renumbered `[1]`…`[7]`, each preceded by a `# wsop/2023/43/5/<file>` comment.
Records are copied verbatim apart from the `[N]` header.

- **Why this subset:** it is the smallest real fixed-limit corpus, and it is an exact oracle: every hand is
  five-handed with antes all 0, blinds 100 000/200 000, `small_bet = 200000`, `big_bet = 400000` and
  `finishing_stacks` recorded. Fixed-limit hands carry no `min_bet`, which the specification forbids.
- **What it verifies:** positions, start stacks, hole cards, the seat on the clock at every decision, street
  progression, every board card and, unlike the handhq corpora, the finishing stacks exactly. The runner creates
  the game as `FixedLimit` with `RaiseCap = 4` and replays `cbr X` as a raise *to* `X`. Players who folded unseen have `????` hole cards, which `PhhHandDeck` fills in.
- **Seating:** five-handed, so it is seated as in full ring: `p1` is the small blind, `p2` the big blind and the
  last player is on the button (`InitialDealerSeatNo = PlayerCount`).

## `wsop-nolimit.phhs.gz`

The eleven hands with `variant = 'NT'` of the same folder, **`data/wsop/2023/43/5`**: `00-02-07`, `00-08-38`,
`00-15-36`, `00-18-39`, `02-51-10`, `02-53-09`, `02-54-12`, `02-56-12`, `02-57-27`, `03-00-32` and `03-02-41`,
ordered by file name and renumbered `[1]`…`[11]`, each preceded by a `# wsop/2023/43/5/<file>` comment. Records are
copied verbatim apart from the `[N]` header. Licence MIT, like the other GitHub subsets.

- **Why this subset:** it is the only real corpus of the dataset with antes that the engine models, and it is an
  exact oracle: every hand is five-handed with `finishing_stacks` recorded and a **big blind ante**, `antes =
  [0, A, 0, 0, 0]` with `p2` the big blind. The ante is one and a half big blinds, not one: 120 000 at blinds
  40 000/80 000 (two hands), 150 000 at 50 000/100 000 (two hands) and 225 000 at 75 000/150 000 (seven hands).
- **What it verifies:** everything `wsop-fixedlimit` does, in no-limit. The runner maps `antes` to
  `GameOptionData.Ante` with `AnteType.BigBlind` (`PhhReplayRunner.AnteFor`; all entries equal would be
  `AnteType.Classic`) and also asserts that the ledger's `Ante` rows are what each seat posted and that
  `HandData.Ante` is the nominal ante. `03-02-41` is the hand where the big blind goes all in with its ante on top
  of a called wager and loses: the ante is dead money in the lowest pot, not an excess returned to the big blind.
- **Seating:** as `wsop-fixedlimit`.

## `acpc-fixedlimit.phhs.gz`

Two files of the Annual Computer Poker Competition logs of 2014, from the **Zenodo edition** of the dataset
(record `17136841`, DOI `10.5281/zenodo.10796885`, licensed **CC-BY-4.0**, not MIT like the GitHub copy; see
[THIRD-PARTY-NOTICES.md](../../../../THIRD-PARTY-NOTICES.md)). The prefix of the entries is
`data/annual-computer-poker-competition/competitions/2014/logs/`:

| Entry | Hands | SHA-256 of the inflated file |
| --- | --- | --- |
| `2pl_logs/2014_2pl/2pl.652.Cleverpiggy.0.0.phhs` | 3 000 heads-up | `27ae7ec6034e313e794588a6e02ca014a4e972f3d1365b26f775c664569c778d` |
| `3pl_logs/2014_3pl/3pl.HITSZ_CS_14.Hyperborean_iro.SmooCT.0.0.phhs` | 3 000 three-player | `fd06609f5a99f249373091cb2fe86ecba78b7183605b3628b9fae61fe8112170` |

The heads-up hands come first, then the three-player ones, renumbered `[1]`…`[6000]`, each file preceded by a
`# acpc/<entry path>` comment. Records are copied verbatim apart from the `[N]` header.

- **Why this slice:** the GitHub copy of the dataset holds only seven fixed-limit hands (`wsop-fixedlimit`). The
  Zenodo archive holds about 340 million ACPC fixed-limit hands, with every hole card dealt as `d dh` and every
  player's net in the custom field `_results`: an exact oracle with no rake. Starting stacks are 2 000, blinds
  5/10 (`small_bet = 10`, `big_bet = 20`), and `Σ _results == 0` in every hand.
- **What it verifies:** positions, hole cards, the seat on the clock at every decision, street progression, every
  board card and the stacks exactly, from `starting_stacks + _results`. Forty three-player results are `2.5`, the
  two-way split of an odd pot, which the engine's rounding to the cent reproduces. No street holds more than four
  bets, the big blind counting pre-flop, so `RaiseCap = 4` is exercised at its limit.
- **What it does not verify:** all-ins (no hand goes all in; the most a hand can lose is 240) and any player count
  beyond 2 and 3.
- **Seating:** heads-up follows the handhq convention above (the last player acts first pre-flop); three-player is
  seated as in full ring.
- **Sampling:** `PhhAcpcFixedLimitReplay_Test` replays an evenly spaced sample of `TCE_TEST_PHH_HAND_LIMIT` hands
  (default 500), so both files are reached; `0` replays all 6 000.

## `pokerkit-potlimit.phhs.gz` and `pokerkit-fixedlimit.phhs.gz`

Two synthetic corpora of 2 000 hands each, played by [pokerkit](https://github.com/uoftcprg/pokerkit) 0.7.7, an
independent MIT implementation of the rules, through `scripts/phh-synthetic/generate.py`. The game is a pokerkit
`State` built from four hold'em `Street`s for both structures, never pokerkit's predefined `FixedLimitTexasHoldem`
(see the divergences below).

- **Why synthetic:** pot-limit Texas hold'em has no PHH data at all. The specification's 11 variant codes have none
  for it (`PO` is pot-limit **Omaha**), and neither the GitHub nor the Zenodo copy of the dataset holds such hands.
  The real fixed-limit corpora above never go all in (ACPC stacks are 200 big blinds and a hand loses at most 240
  chips; the WSOP hands are five-handed), so nothing real covers side pots, incomplete all-in raises or 4 to 10
  seats in fixed-limit.
- **How they are generated:** in `docker run --rm` on `python:3.12-slim@sha256:5e2bdb1b0d542d5039cffb8181c65930093949194a65c365dfb1b0fe13a4fa5b`
  with `pokerkit==0.7.7`, nothing installed on the host. One `random.Random(seed)` drives every draw, so a seed gives
  byte-identical output: seed **657** for pot-limit and **658** for fixed-limit. Seat counts run 2 to 10 in turn
  (223 hands of 2 and of 3 seats, 222 of every other count). Blinds are 50/100 in whole chips; fixed-limit has
  `small_bet = 100` and `big_bet = 200`, pot-limit `min_bet = 100`. Each seat's stack is drawn from one of three
  bands, deep 100–300, medium 20–60 or short 1–10 big blinds; about a third of the hands hold at least one short
  seat. The policy picks among the actions pokerkit allows with weights fold 0.15, check/call 0.5 and bet/raise
  0.35; a pot-limit raise is a uniform whole-chip size inside pokerkit's window, a fixed-limit raise the one size
  offered. 1 608 pot-limit hands and 504 fixed-limit hands contain an all-in.
- **What is in a hand:** every hole card is dealt as `d dh`, every board card as `d db`, and every hand reaching a
  showdown shows all its hole cards as `pN sm <cards>`. `cbr X` is a raise *to* `X`. Each table carries `variant`,
  `antes` (zeros), `blinds_or_straddles` (as passed to pokerkit), `small_bet`/`big_bet` or `min_bet`,
  `starting_stacks`, `actions`, `finishing_stacks` and the custom fields `_generator = 'pokerkit 0.7.7'` and `_seed`.
  The generator checks `Σ finishing_stacks == Σ starting_stacks` for every hand and, for fixed-limit, loads 200
  evenly spaced written hands back with `pokerkit.HandHistory.loads` and replays them to the same finishing stacks.
- **The non-standard `PT` code:** the pot-limit hands carry `variant = 'PT'`, a code this project made up for
  pot-limit Texas hold'em by analogy with `NT` and `FT`. It is not in the PHH specification, so pokerkit's own
  reader and other PHH tools reject these hands. The fixed-limit hands carry the standard `FT`.
- **Seating:** `p1` is the small blind and the last player is on the button, except heads-up, where pokerkit
  reverses the blinds exactly as the handhq and ACPC histories do (`p1` is the big blind, the last player posts the
  small blind and acts first pre-flop).

### Where pokerkit and this engine differ

- **The pre-flop cap.** pokerkit's predefined `FixedLimitTexasHoldem` allows four raises over the big blind
  pre-flop; the engine allows a bet and three raises, the big blind being the bet. The generator avoids it by
  capping its `Street`s at three raises pre-flop and four on every later street.
- **Incomplete all-in raises.** The engine follows TDA Rule 47 in every structure: an all-in for less than a full
  raise does not reopen the betting to a seat that has already acted. pokerkit reopens it. This cannot be
  configured away, so a hand where it can matter carries `_tce_divergence = 'incomplete-all-in'`: on some street a
  seat went all in with a raise or bet smaller than a full raise (the last full raise on the street, at least the
  big blind, and for a first bet less than the minimum bet), and then a seat that had already acted on that street
  acted again. In fixed-limit pokerkit also counts every all-in raise toward the cap, where the engine counts an
  all-in raise of at least half a bet and lets it reopen the betting; there a hand also carries the tag when the
  street reached the cap after such a raise. 697 of the 2 000 pot-limit hands and 177 of the 2 000 fixed-limit
  hands are tagged. The policy does not avoid these spots, because hiding them would hide the difference.
- **Odd chips of a split pot.** pokerkit splits a pot in whole chips and gives the remainder to one winner; the
  engine rounds each share to the cent. A hand where a split pot does not divide evenly therefore ends with
  different stacks, and carries `_tce_divergence = 'odd-chip'`: one push of the pot gives unequal non-zero amounts
  to its winners. 78 of the pot-limit hands and 14 of the fixed-limit hands are tagged. A hand with both
  divergences carries both names, `'incomplete-all-in, odd-chip'` (36 and 1 hands).

The replays skip every hand that carries `_tce_divergence` through `PhhReplayRunner.IsSupported`.

### Licence

pokerkit is a tool that was run to produce these files; it is not redistributed, and no part of it is in the
repository. The generated hands are this project's own work, so `THIRD-PARTY-NOTICES.md` has no row for them.

## `pokerkit-antes.phhs.gz`

2 000 synthetic no-limit hands (`variant = 'NT'`) with antes, played by pokerkit 0.7.7 through the same
`scripts/phh-synthetic/generate.py` as the two corpora above (same image, same stack bands, same action policy, raises a
uniform whole-chip size inside pokerkit's window). The real ante oracle (`wsop-nolimit.phhs.gz`) is eleven five-handed
hands with a big blind ante and deep stacks; nothing real has classic antes, a seat all in on its ante, a big blind
short of its ante, or side pots built from antes at 2 to 10 seats.

- **Two runs, concatenated:** hands 1 to 1 000 are `--antes classic` with seed **659** (every seat posts the ante,
  `ante_trimming_status = true`), hands 1 001 to 2 000 are `--antes big-blind` with seed **660** (the big blind's player
  alone posts it, `ante_trimming_status = false`, which pokerkit documents for non-uniform antes). The second run
  uses `--first-number 1001 --append`, so the file is numbered `[1]` to `[2000]` and every hand keeps the `_generator` and
  `_seed` of its own run. Seat counts run 2 to 10 in turn in each half. `--antes` defaults to `none`, which writes the
  zeros and draws nothing extra, so the pot-limit and fixed-limit files above regenerate byte for byte.
- **The ante:** drawn per hand as a ratio of the big blind (100): 0.1, 0.25, 0.5, 1 or 1.5, that is 10, 25, 50, 100 or
  150 chips. In one hand in five a seat (in the big blind ante half, the big blind's player in half of those) gets a stack
  drawn from 1 to one ante plus one big blind minus one, so a seat is put all in by its ante, or the big blind falls
  short of its blind plus its ante. `antes` is written as passed to pokerkit.
- **Heads-up `antes` are pokerkit's order, not the seating order.** pokerkit indexes `antes` like
  `blinds_or_straddles` and reverses the two entries heads-up (`State.get_effective_ante`), so the big blind's ante,
  paid by `p1`, is written `antes = [0, a]`: the second entry in every table size. Read by seating order, as the
  specification has it, that ante looks like it belongs to `p2`, the button.
- **How it is checked:** the generator checks chip conservation for every hand and loads every written hand back with
  `pokerkit.HandHistory.loads`, replaying it to the same finishing stacks.
- **Commands** (the image and pokerkit version of the section above, from the repository root; `$OUT` is a writable folder):

```
python /gen/generate.py --structure no-limit --antes classic --hands 1000 --seed 659 --out /out/pokerkit-antes.phhs
python /gen/generate.py --structure no-limit --antes big-blind --hands 1000 --seed 660 --first-number 1001 --append --out /out/pokerkit-antes.phhs
```

The result is then gzipped like the other files (`System.IO.Compression.GZipStream`, optimal).

### Where pokerkit and this engine differ

- **A big blind short of its blind plus its ante.** pokerkit collects the antes before the blinds, so a big blind with
  `S < ante + big blind` posts the ante first and the blind from what is left; ADR-0011 posts the big blind first and the
  ante from what is left. The two split the same chips differently (stack 150, ante 100: pokerkit posts a 100 ante and
  a 50 blind, the engine a 100 blind and a 50 ante), which changes what the others must call. A hand of the big blind
  half where the big blind's stack is below the ante plus 100 carries
  `_tce_divergence = 'big blind ante posted before the blind'`.
- **Incomplete all-in raises and odd chips** are the two reasons of the pokerkit corpora above and are tagged the same
  way, with the same names, joined by a comma when a hand has several.
- **Counts.** 845 of the 2 000 hands are tagged: 382 of the classic half and 463 of the big blind half. By reason, 716
  carry `incomplete-all-in` (364 and 352), 72 carry `odd-chip` (36 and 36) and 131 carry
  `big blind ante posted before the blind` (90 of them with no other reason). 238 hands have a seat all in on the ante it
  posted (124 classic, 114 big blind) and 931 have a side pot (472 and 459).
- **Not tagged, and the engine agrees.** In 79 hands of the classic half a seat's stack is no more than the ante, so the
  ante empties it before the blinds; in others a seat is short of its blind. pokerkit keeps the blind seats by position
  and posts what the seat has, a blind of 0 for an empty one, and so does the engine since #744 (fact 32 of
  `docs/task-run.md`).
- **A check nobody can answer.** When every other seat is all in or folded and the last seat with chips already holds
  the standing wager (a blind its ante emptied posted nothing), pokerkit still asks that seat for a `cc`. The engine puts
  nobody on the clock there, which is the same hand, and `PhhReplayRunner` skips that one check; the stacks are still
  compared exactly.

The replays skip every hand that carries `_tce_divergence` through `PhhReplayRunner.IsSupported`.

**Replayed by** `PhhPokerkitAntesReplay_Test` in `PhhReplayTests`: the 1 155 supported hands, never sampled by
`TCE_TEST_PHH_HAND_LIMIT`, with the stacks compared exactly against `finishing_stacks`.
`PhhPokerkitAntesCorpus_ParsesAndIsMostlySupported` pins the 2 000 hands and the 845 tagged ones.

## Format

See the [PHH specification](https://phh.readthedocs.io/en/stable/spec.html). The action grammar used
by both corpora is `d dh p<n> <cards>` (deal hole cards), `d db <cards>` (deal board),
`p<n> f` (fold), `p<n> cc` (check/call), `p<n> cbr <amount>` (complete/bet/raise **to** `amount`)
and `p<n> sm [cards]` (show/muck). Per the specification, `p1` is the small blind and the last player
has the button — with the heads-up caveat above. Pluribus writes every hole card as `d dh`; handhq
writes only what was shown, as `sm`, and `PhhHandDeck` reads both.

## Regenerating

```
pwsh ./scripts/pack-phh-fixture.ps1
pwsh ./scripts/pack-phh-headsup-fixture.ps1
pwsh ./scripts/pack-phh-fullring-fixture.ps1
pwsh ./scripts/pack-phh-wsop-fixedlimit-fixture.ps1
pwsh ./scripts/pack-phh-acpc-fixture.ps1
pwsh ./scripts/generate-phh-synthetic-fixture.ps1
pwsh ./scripts/pack-phh-wsop-nolimit-fixture.ps1
```

The seventh packs `wsop-nolimit.phhs.gz` the same way the fourth packs `wsop-fixedlimit.phhs.gz`, keeping `NT`
instead of `FT`, and reproduces the committed file byte for byte.

The sixth regenerates both `pokerkit-*` files with the pinned image, pokerkit version and seeds above, and prints the
hand counts per seat count, the hands with an all-in and the tagged fixed-limit hands. It needs PowerShell 7 and
Docker, and reproduces the committed files byte for byte.

The fourth packs `wsop-fixedlimit.phhs.gz` from the pinned commit, reading the files with `git cat-file`.
The fifth packs `acpc-fixedlimit.phhs.gz` from the Zenodo archive without downloading it: it reads the ZIP64 central
directory and the two entries with HTTP Range requests, and checks size, CRC-32 and the SHA-256 values above, stopping on
any mismatch. It needs PowerShell 7 and network access to zenodo.org.

The first sparse-clones the upstream repository (a full clone is ~1.7 GB; a blobless sparse checkout
of `data/pluribus` is ~12 MB), repacks `pluribus.phhs.gz` and refreshes `LICENSE-phh-dataset.txt`.
The second packs `handhq-headsup.phhs.gz`, and the third packs `handhq-fullring.phhs.gz`; both read
their handful of upstream files with `git cat-file` rather than checking them out, because a checkout
of `data/handhq` runs to several gigabytes. Update the commit SHA above afterwards. None of the scripts
are part of the build or CI.

There are three scripts rather than one switch because the subsets differ in shape on disk:
`data/pluribus` is one `.phh` file per hand, while `data/handhq` is `.phhs` files holding a thousand
hands each, so packing means splitting records out and renumbering them. Different subsets within
`data/handhq` also need different filters (heads-up vs full-ring).

## Citation

> Kim, J. (2024). *Recording and Describing Poker Hands.* arXiv:2312.11753.
> <https://arxiv.org/abs/2312.11753>

> Universal, Open, Free, and Transparent Computer Poker Research Group. *A Dataset of Poker Hand
> Histories.* <https://doi.org/10.5281/zenodo.10796885> (licensed CC-BY-4.0; the ACPC logs it holds come from the
> Annual Computer Poker Competition).

The upstream hands originate from the supplementary material of Brown, N. & Sandholm, T. (2019).
*Superhuman AI for multiplayer poker.* Science 365(6456), 885–890.
