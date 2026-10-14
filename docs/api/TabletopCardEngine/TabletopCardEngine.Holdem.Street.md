#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## Street Enum

A betting round\.

The values are <b>not</b> sequential: [Flop](TabletopCardEngine.Holdem.Street.md#TabletopCardEngine.Holdem.Street.Flop 'TabletopCardEngine\.Holdem\.Street\.Flop'), [Turn](TabletopCardEngine.Holdem.Street.md#TabletopCardEngine.Holdem.Street.Turn 'TabletopCardEngine\.Holdem\.Street\.Turn') and
[River](TabletopCardEngine.Holdem.Street.md#TabletopCardEngine.Holdem.Street.River 'TabletopCardEngine\.Holdem\.Street\.River') encode how many community cards are on the board, and code relies on that.
Use `StreetExtensions.Next()` to move between streets rather than incrementing.

```csharp
public enum Street : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.Street.PreFlop'></a>

`PreFlop` 0

Before the flop; no community cards\.

<a name='TabletopCardEngine.Holdem.Street.Flop'></a>

`Flop` 3

Three community cards\.

<a name='TabletopCardEngine.Holdem.Street.Turn'></a>

`Turn` 4

Four community cards\.

<a name='TabletopCardEngine.Holdem.Street.River'></a>

`River` 5

Five community cards\.

<a name='TabletopCardEngine.Holdem.Street.Showdown'></a>

`Showdown` 6

Betting is over and the remaining hands are compared\.

<a name='TabletopCardEngine.Holdem.Street.Finished'></a>

`Finished` 7

The hand is settled and the pot has been paid out\.
