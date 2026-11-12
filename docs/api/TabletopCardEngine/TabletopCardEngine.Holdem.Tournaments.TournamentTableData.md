#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentTableData Class

One table of a tournament: an ordinary game, played through [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable') with GameId\.

```csharp
public record TournamentTableData : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentTableData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentTableData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentTableData](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TournamentTableData(int,System.Guid)'></a>

## TournamentTableData\(int, Guid\) Constructor

One table of a tournament: an ordinary game, played through [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable') with [GameId](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md#TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TournamentTableData(int,System.Guid).GameId 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData\.TournamentTableData\(int, System\.Guid\)\.GameId')\.

```csharp
public TournamentTableData(int TableNo, System.Guid GameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TournamentTableData(int,System.Guid).TableNo'></a>

`TableNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The table's number within the tournament, ascending from 1\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TournamentTableData(int,System.Guid).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game the table is played as\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.GameId'></a>

## TournamentTableData\.GameId Property

The game the table is played as\.

```csharp
public System.Guid GameId { get; init; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.IsBroken'></a>

## TournamentTableData\.IsBroken Property

Whether the table has been broken up and its players moved to other tables; a broken table deals no more hands\.

```csharp
public bool IsBroken { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.PlayerCount'></a>

## TournamentTableData\.PlayerCount Property

How many players are seated at the table\.

```csharp
public int PlayerCount { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TableNo'></a>

## TournamentTableData\.TableNo Property

The table's number within the tournament, ascending from 1\.

```csharp
public int TableNo { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
