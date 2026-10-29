# PHH test fixtures

Two vendored corpora, both from the [PHH dataset](https://github.com/uoftcprg/phh-dataset) at commit
`e47fbd5816372360bade4de5d712346fe1bb70f6` (MIT — see
[LICENSE-phh-dataset.txt](LICENSE-phh-dataset.txt)):

| File | Hands | What it is |
| --- | --- | --- |
| `pluribus.phhs.gz` | 10 000, all replayable | Bot play, six-handed. Exact oracle. |
| `handhq-headsup.phhs.gz` | 2 351, of which 2 145 replayable | Real online play, heads-up. Structural oracle. |
| `handhq-fullring.phhs.gz` | 1 505, of which 1 505 replayable | Real online play, full-ring (7–10 seated). Structural oracle. |

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
  (`StartHandActionHandler` derives the big blind as `InitialSmallBlind * 2` and has no ante concept).

Stacks are reset to 10 000 before every hand, so each hand is replayed as its own `Game`.

## `handhq-headsup.phhs.gz`

The heads-up hands of the first five files of **`data/handhq/PS-2009-07-01_2009-07-23_50NLH_OBFU/0.5`**
— anonymised PokerStars \$0.25/\$0.50 no-limit hold'em logs scraped in July 2009. Only hands whose
`blinds_or_straddles` has exactly two entries are packed; that is a mechanical "is this heads-up"
test, and every other requirement is decided at test time by `PhhReplayRunner.IsSupported`, so the
filter has one home. Records are copied verbatim apart from the renumbered `[N]` header.

The `pluribus` subset is entirely six-handed, and it is the only subset of the dataset that is both
no-limit hold'em and an exact oracle. Heads-up needed its own corpus:

- the WSOP subset's 11 no-limit hold'em hands are all five-handed with a big-blind ante;
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
```

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
> Histories.* <https://doi.org/10.5281/zenodo.10796885>

The upstream hands originate from the supplementary material of Brown, N. & Sandholm, T. (2019).
*Superhuman AI for multiplayer poker.* Science 365(6456), 885–890.
