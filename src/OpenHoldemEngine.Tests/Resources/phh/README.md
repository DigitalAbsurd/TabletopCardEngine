# PHH test fixture

`pluribus.phhs.gz` is a vendored copy of the **`data/pluribus`** subset of the
[PHH dataset](https://github.com/uoftcprg/phh-dataset), packed into a single gzipped
[`.phhs`](https://pokerkit.readthedocs.io/en/stable/notation.html) file (TOML array of tables with
numeric headers `[1]`, `[2]`, … — the layout `pokerkit`'s `HandHistory.dump_all` emits, so the
fixture stays readable by third-party PHH tooling). Each table is preceded by a comment naming the
upstream file it came from.

- **Upstream:** <https://github.com/uoftcprg/phh-dataset>
- **Upstream commit:** `e47fbd5816372360bade4de5d712346fe1bb70f6`
- **License:** MIT — see [LICENSE-phh-dataset.txt](LICENSE-phh-dataset.txt)
- **Contents:** 10 000 hands (5.9 MB uncompressed), all 6-handed no-limit Texas hold'em,
  blinds 50/100, no antes, every stack starting at 10 000

## Why this subset

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

## Format

See the [PHH specification](https://phh.readthedocs.io/en/stable/spec.html). The action grammar used
by this subset is `d dh p<n> <cards>` (deal hole cards), `d db <cards>` (deal board),
`p<n> f` (fold), `p<n> cc` (check/call), `p<n> cbr <amount>` (complete/bet/raise **to** `amount`)
and `p<n> sm [cards]` (show/muck). Per the specification, `p1` is the small blind and the
last player has the button.

## Regenerating

```
pwsh ./scripts/pack-phh-fixture.ps1
```

The script sparse-clones the upstream repository (a full clone is ~1.7 GB; a blobless sparse
checkout of `data/pluribus` is ~12 MB), repacks the fixture and refreshes
`LICENSE-phh-dataset.txt`. Update the commit SHA above afterwards. The script is not part of the
build or CI.

## Citation

> Kim, J. (2024). *Recording and Describing Poker Hands.* arXiv:2312.11753.
> <https://arxiv.org/abs/2312.11753>

> Universal, Open, Free, and Transparent Computer Poker Research Group. *A Dataset of Poker Hand
> Histories.* <https://doi.org/10.5281/zenodo.10796885>

The upstream hands originate from the supplementary material of Brown, N. & Sandholm, T. (2019).
*Superhuman AI for multiplayer poker.* Science 365(6456), 885–890.
