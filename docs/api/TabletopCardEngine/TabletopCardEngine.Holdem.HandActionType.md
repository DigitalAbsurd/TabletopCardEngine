#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HandActionType Enum

What the hand's ledger records for one action\. It is [PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType') plus the two
forced blind posts, which the engine places itself and a caller can never request\. The ledger
stores the action the caller asked for, so a request that the rules turned into something else
\(an unaffordable call, say\) is still recorded as requested\.

```csharp
public enum HandActionType : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.HandActionType.SmallBlind'></a>

`SmallBlind` 1

The forced small\-blind post, placed by the engine when a hand starts\.

<a name='TabletopCardEngine.Holdem.HandActionType.BigBlind'></a>

`BigBlind` 2

The forced big\-blind post, placed by the engine when a hand starts\.

<a name='TabletopCardEngine.Holdem.HandActionType.Fold'></a>

`Fold` 3

The seat gives up its claim on the pot\.

<a name='TabletopCardEngine.Holdem.HandActionType.Check'></a>

`Check` 4

The seat stays in without adding chips\.

<a name='TabletopCardEngine.Holdem.HandActionType.Call'></a>

`Call` 5

The seat matches the standing wager\.

<a name='TabletopCardEngine.Holdem.HandActionType.Raise'></a>

`Raise` 6

The seat raises the street wager to the recorded total\.

<a name='TabletopCardEngine.Holdem.HandActionType.Bet'></a>

`Bet` 8

An opening wager on a street where nothing has been bet\.

<a name='TabletopCardEngine.Holdem.HandActionType.AllIn'></a>

`AllIn` 9

The seat has committed its whole stack\.
