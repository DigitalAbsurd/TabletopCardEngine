#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## BettingStructure Enum

How bets and raises are sized in a game\.

```csharp
public enum BettingStructure : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.BettingStructure.NoLimit'></a>

`NoLimit` 1

No\-Limit: any bet or raise from the minimum up to the whole stack\.

<a name='TabletopCardEngine.Holdem.BettingStructure.PotLimit'></a>

`PotLimit` 2

Pot\-Limit: any bet or raise from the minimum up to the size of the pot after a call\.

<a name='TabletopCardEngine.Holdem.BettingStructure.FixedLimit'></a>

`FixedLimit` 3

Fixed\-Limit: bets and raises of one fixed size: the big blind pre\-flop and on the flop, two big blinds on the
turn and river, at most [RaiseCap](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.RaiseCap 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.RaiseCap') bets a street\.
