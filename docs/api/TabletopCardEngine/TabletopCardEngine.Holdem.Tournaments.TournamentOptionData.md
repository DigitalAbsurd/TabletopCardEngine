#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentOptionData Class

The rules a tournament is created with; they never change afterwards\. Build it with an object initializer: every
option has a default, so a new option can be added without breaking a caller\. A freezeout: no rebuys, add\-ons or
late registration\.

Record equality compares [Levels](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Levels 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.Levels') and [Payouts](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Payouts 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.Payouts') element by element for values the engine
returns, so the options read back from a tournament equal the ones it was created with.

```csharp
public sealed record TournamentOptionData : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentOptionData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentOptionData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.AnteType'></a>

## TournamentOptionData\.AnteType Property

Who posts the ante of a level that has one\. Must be a defined [AnteType](TabletopCardEngine.Holdem.AnteType.md 'TabletopCardEngine\.Holdem\.AnteType') value\.

```csharp
public TabletopCardEngine.Holdem.AnteType AnteType { get; init; }
```

#### Property Value
[AnteType](TabletopCardEngine.Holdem.AnteType.md 'TabletopCardEngine\.Holdem\.AnteType')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.BettingStructure'></a>

## TournamentOptionData\.BettingStructure Property

How bets are sized at every table\. Must be a structure the engine plays, as for a cash game\.

```csharp
public TabletopCardEngine.Holdem.BettingStructure BettingStructure { get; init; }
```

#### Property Value
[BettingStructure](TabletopCardEngine.Holdem.BettingStructure.md 'TabletopCardEngine\.Holdem\.BettingStructure')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.BuyIn'></a>

## TournamentOptionData\.BuyIn Property

What each entry adds to the prize pool, which is this times the number of entries; `0` \(the default\) makes
a free tournament\. A fee on top of it is the host's business\. Must not be negative and must have at most two decimal places\.

```csharp
public decimal BuyIn { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Levels'></a>

## TournamentOptionData\.Levels Property

The blind levels in the order they are played\. Must hold 1 to 100 levels whose small blinds never decrease from
one level to the next; see [TournamentLevelData](TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentLevelData') for each level's rules\. The levels together are
stored in one column, so they must also fit in 4 000 characters of JSON\. Empty by default, so a caller has to
set them\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Tournaments.TournamentLevelData> Levels { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[TournamentLevelData](TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentLevelData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxEntries'></a>

## TournamentOptionData\.MaxEntries Property

The most entries registration takes; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') \(the default\) means no limit\. When set it must be at least [MinEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MinEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MinEntries')\.

```csharp
public System.Nullable<int> MaxEntries { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxPlayersPerTable'></a>

## TournamentOptionData\.MaxPlayersPerTable Property

The seats of every table; must lie in the range of [TableFormat](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.TableFormat 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.TableFormat')\.

```csharp
public int MaxPlayersPerTable { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MinEntries'></a>

## TournamentOptionData\.MinEntries Property

The fewest entries the tournament can start with\. Must be at least 2\.

```csharp
public int MinEntries { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Payouts'></a>

## TournamentOptionData\.Payouts Property

The share of the prize pool each place wins, in percent: first place first, then second, and so on\. Must hold
1 to [MinEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MinEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MinEntries') values, each positive with at most two decimal places, none larger than the one
before it, summing to exactly 100\. Defaults to `[100]`: winner takes all\.

```csharp
public System.Collections.Generic.IReadOnlyList<decimal> Payouts { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.RaiseCap'></a>

## TournamentOptionData\.RaiseCap Property

The most bets a street may hold under [FixedLimit](TabletopCardEngine.Holdem.BettingStructure.md#TabletopCardEngine.Holdem.BettingStructure.FixedLimit 'TabletopCardEngine\.Holdem\.BettingStructure\.FixedLimit'), as for a cash game:
[null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') means no cap, otherwise it must be at least 1\. Ignored by the other structures\.

```csharp
public System.Nullable<int> RaiseCap { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.StartingStack'></a>

## TournamentOptionData\.StartingStack Property

The chips every player starts with\. Must be positive and have at most two decimal places\.

```csharp
public decimal StartingStack { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.TableFormat'></a>

## TournamentOptionData\.TableFormat Property

The format of every table, which caps [MaxPlayersPerTable](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxPlayersPerTable 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MaxPlayersPerTable'): [HeadsUp](TabletopCardEngine.Holdem.HoldemTableFormat.md#TabletopCardEngine.Holdem.HoldemTableFormat.HeadsUp 'TabletopCardEngine\.Holdem\.HoldemTableFormat\.HeadsUp') is exactly 2, [SixMax](TabletopCardEngine.Holdem.HoldemTableFormat.md#TabletopCardEngine.Holdem.HoldemTableFormat.SixMax 'TabletopCardEngine\.Holdem\.HoldemTableFormat\.SixMax') 2 to 6 and [FullRing](TabletopCardEngine.Holdem.HoldemTableFormat.md#TabletopCardEngine.Holdem.HoldemTableFormat.FullRing 'TabletopCardEngine\.Holdem\.HoldemTableFormat\.FullRing') 7 to 10\. Must be a defined value\.

```csharp
public TabletopCardEngine.Holdem.HoldemTableFormat TableFormat { get; init; }
```

#### Property Value
[HoldemTableFormat](TabletopCardEngine.Holdem.HoldemTableFormat.md 'TabletopCardEngine\.Holdem\.HoldemTableFormat')
