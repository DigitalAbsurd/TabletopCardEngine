#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## PlayerActionType Enum

What a caller may ask a seat to do\. The engine also records the forced blind posts, but those
are never requested, so they live only in [HandActionType](TabletopCardEngine.Holdem.HandActionType.md 'TabletopCardEngine\.Holdem\.HandActionType')\.

```csharp
public enum PlayerActionType : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.PlayerActionType.Fold'></a>

`Fold` 3

The seat gives up its claim on the pot\.

<a name='TabletopCardEngine.Holdem.PlayerActionType.Check'></a>

`Check` 4

The seat stays in without adding chips; legal only when there is nothing to call\.

<a name='TabletopCardEngine.Holdem.PlayerActionType.Call'></a>

`Call` 5

The seat matches the standing wager\.

<a name='TabletopCardEngine.Holdem.PlayerActionType.Raise'></a>

`Raise` 6

The seat raises the street wager\. Its size is the total to raise &lt;i&gt;to&lt;/i&gt;, not the increment\.

<a name='TabletopCardEngine.Holdem.PlayerActionType.Bet'></a>

`Bet` 8

An opening wager on a street where nothing has been bet\.

<a name='TabletopCardEngine.Holdem.PlayerActionType.AllIn'></a>

`AllIn` 9

Puts the seat's whole stack in: a call when the stack does not exceed the amount to call, otherwise a raise to the full stack\.
