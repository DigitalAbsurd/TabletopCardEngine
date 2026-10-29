#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemTableFormat Enum

The table format of a No\-Limit game, chosen when the game is created: how many seats the table
is built for\. It belongs to the game and is stored with it\.

```csharp
public enum HoldemTableFormat : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.HoldemTableFormat.HeadsUp'></a>

`HeadsUp` 1

Heads\-Up \(2\-Max\): one\-on\-one play, exactly 2 seats\.

<a name='TabletopCardEngine.Holdem.HoldemTableFormat.SixMax'></a>

`SixMax` 2

6\-Max: a short\-handed table of 2 to 6 seats\. The default format\.

<a name='TabletopCardEngine.Holdem.HoldemTableFormat.FullRing'></a>

`FullRing` 3

Full Ring: the traditional full\-size table of 7 to 10 seats, most often nine\.
