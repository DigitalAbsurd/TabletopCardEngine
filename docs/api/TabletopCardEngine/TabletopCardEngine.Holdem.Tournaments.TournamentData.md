#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentData Class

Everything the engine knows about a tournament, as returned by every call\.

```csharp
public record TournamentData : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentData](TabletopCardEngine.Holdem.Tournaments.TournamentData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentData(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,TabletopCardEngine.Holdem.Tournaments.TournamentState,int)'></a>

## TournamentData\(Guid, TournamentOptionData, TournamentState, int\) Constructor

Everything the engine knows about a tournament, as returned by every call\.

```csharp
public TournamentData(System.Guid TournamentId, TabletopCardEngine.Holdem.Tournaments.TournamentOptionData Options, TabletopCardEngine.Holdem.Tournaments.TournamentState State, int Version);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentData(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,TabletopCardEngine.Holdem.Tournaments.TournamentState,int).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

Identifier of the tournament\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentData(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,TabletopCardEngine.Holdem.Tournaments.TournamentState,int).Options'></a>

`Options` [TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')

The rules the tournament was created with\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentData(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,TabletopCardEngine.Holdem.Tournaments.TournamentState,int).State'></a>

`State` [TournamentState](TabletopCardEngine.Holdem.Tournaments.TournamentState.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState')

Where the tournament is in its life\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentData(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,TabletopCardEngine.Holdem.Tournaments.TournamentState,int).Version'></a>

`Version` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

How many times the tournament has been saved, starting at `1` on creation\. Pass it back as
[ExpectedVersion](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md#TabletopCardEngine.Holdem.Tournaments.TournamentAction.ExpectedVersion 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction\.ExpectedVersion') to have a stale write rejected with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict')\.
Reading the tournament does not change it\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.Entries'></a>

## TournamentData\.Entries Property

Every registered player, ascending by [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo')\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Tournaments.TournamentEntryData> Entries { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[TournamentEntryData](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.FinishedAt'></a>

## TournamentData\.FinishedAt Property

When the tournament finished; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') until it has\.

```csharp
public System.Nullable<System.DateTimeOffset> FinishedAt { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.LevelEndsAt'></a>

## TournamentData\.LevelEndsAt Property

When the current level ends; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') before the start and on the last level, which lasts until the tournament ends\.

```csharp
public System.Nullable<System.DateTimeOffset> LevelEndsAt { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.LevelIndex'></a>

## TournamentData\.LevelIndex Property

The current blind level, as an index into [Levels](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Levels 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.Levels'): `0` is the first level, as for `HandData.BlindLevel`\.

```csharp
public int LevelIndex { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.Options'></a>

## TournamentData\.Options Property

The rules the tournament was created with\.

```csharp
public TabletopCardEngine.Holdem.Tournaments.TournamentOptionData Options { get; init; }
```

#### Property Value
[TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.PrizePool'></a>

## TournamentData\.PrizePool Property

What the places share: [BuyIn](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.BuyIn 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.BuyIn') times the number of entries\.

```csharp
public decimal PrizePool { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.StartedAt'></a>

## TournamentData\.StartedAt Property

When the tournament started; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') while it is registering\.

```csharp
public System.Nullable<System.DateTimeOffset> StartedAt { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset 'System\.DateTimeOffset')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.State'></a>

## TournamentData\.State Property

Where the tournament is in its life\.

```csharp
public TabletopCardEngine.Holdem.Tournaments.TournamentState State { get; init; }
```

#### Property Value
[TournamentState](TabletopCardEngine.Holdem.Tournaments.TournamentState.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.Tables'></a>

## TournamentData\.Tables Property

The tournament's tables, ascending by [TableNo](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md#TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TableNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData\.TableNo'); empty until the tournament starts\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Tournaments.TournamentTableData> Tables { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[TournamentTableData](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.TournamentId'></a>

## TournamentData\.TournamentId Property

Identifier of the tournament\.

```csharp
public System.Guid TournamentId { get; init; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentData.Version'></a>

## TournamentData\.Version Property

How many times the tournament has been saved, starting at `1` on creation\. Pass it back as
[ExpectedVersion](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md#TabletopCardEngine.Holdem.Tournaments.TournamentAction.ExpectedVersion 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction\.ExpectedVersion') to have a stale write rejected with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict')\.
Reading the tournament does not change it\.

```csharp
public int Version { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
