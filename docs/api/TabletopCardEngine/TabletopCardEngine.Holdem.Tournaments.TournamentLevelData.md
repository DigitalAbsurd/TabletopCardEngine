#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentLevelData Class

One blind level of a tournament\. The levels run on a clock from the start of the tournament, and a hand keeps the
level it was dealt at\. The big blind is twice [SmallBlind](TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.md#TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.SmallBlind 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentLevelData\.SmallBlind')\. The last level lasts until the tournament
ends, whatever its [Duration](TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.md#TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.Duration 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentLevelData\.Duration')\.

```csharp
public sealed record TournamentLevelData : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentLevelData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentLevelData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentLevelData](TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentLevelData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.Ante'></a>

## TournamentLevelData\.Ante Property

The ante of the level, posted as [AnteType](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.AnteType 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.AnteType') says; `0` \(the default\) means none\. Must not be negative and must have at most two decimal places\.

```csharp
public decimal Ante { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.Duration'></a>

## TournamentLevelData\.Duration Property

How long the level lasts\. Must be at least one minute and a whole number of seconds\.

```csharp
public System.TimeSpan Duration { get; init; }
```

#### Property Value
[System\.TimeSpan](https://learn.microsoft.com/en-us/dotnet/api/system.timespan 'System\.TimeSpan')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentLevelData.SmallBlind'></a>

## TournamentLevelData\.SmallBlind Property

The small blind of the level\. Must be positive, have at most two decimal places, and not be smaller than the previous level's\.

```csharp
public decimal SmallBlind { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
