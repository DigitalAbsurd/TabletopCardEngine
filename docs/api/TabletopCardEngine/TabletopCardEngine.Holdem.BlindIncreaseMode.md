#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## BlindIncreaseMode Enum

How a game's small blind is worked out each time its blind schedule raises a level\.

```csharp
public enum BlindIncreaseMode : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.BlindIncreaseMode.Multiplier'></a>

`Multiplier` 1

Each level's small blind is the previous one times the schedule's multiplier, rounded to the cent\. The default\.

<a name='TabletopCardEngine.Holdem.BlindIncreaseMode.Levels'></a>

`Levels` 2

Each level's small blind is the next entry of the schedule's list of small blinds; the last one holds once the list is exhausted\.
