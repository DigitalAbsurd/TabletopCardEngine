#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HandRankType Enum

The display category of a made hand, ascending in strength\. This is for showing the result only —
two hands of the same category are ranked against each other by their strength value, not by this\.

```csharp
public enum HandRankType : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.HandRankType.None'></a>

`None` 0

No hand has been evaluated\.

<a name='TabletopCardEngine.Holdem.HandRankType.HighCard'></a>

`HighCard` 1

No pair or better\.

<a name='TabletopCardEngine.Holdem.HandRankType.Pair'></a>

`Pair` 2

One pair\.

<a name='TabletopCardEngine.Holdem.HandRankType.TwoPair'></a>

`TwoPair` 3

Two pair\.

<a name='TabletopCardEngine.Holdem.HandRankType.Trips'></a>

`Trips` 4

Three of a kind\.

<a name='TabletopCardEngine.Holdem.HandRankType.Straight'></a>

`Straight` 5

Five cards in sequence\.

<a name='TabletopCardEngine.Holdem.HandRankType.Flush'></a>

`Flush` 6

Five cards of one suit\.

<a name='TabletopCardEngine.Holdem.HandRankType.FullHouse'></a>

`FullHouse` 7

Three of a kind plus a pair\.

<a name='TabletopCardEngine.Holdem.HandRankType.FourOfAKind'></a>

`FourOfAKind` 8

Four of a kind\.

<a name='TabletopCardEngine.Holdem.HandRankType.StraightFlush'></a>

`StraightFlush` 9

Five suited cards in sequence\.
