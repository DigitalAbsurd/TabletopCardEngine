#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionUnregister Class

Withdraws a registration, while the tournament is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') only; afterwards it
fails with [TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted')\. An EntryNo that is not
registered fails with [InvalidEntry](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidEntry 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidEntry')\. The entry leaves
[Entries](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.Entries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.Entries') and its buy\-in leaves the [PrizePool](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.PrizePool 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.PrizePool')\.

```csharp
public record TournamentActionUnregister : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionUnregister

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionUnregister](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.TournamentActionUnregister(System.Guid,int)'></a>

## TournamentActionUnregister\(Guid, int\) Constructor

Withdraws a registration, while the tournament is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') only; afterwards it
fails with [TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted')\. An [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md#TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.TournamentActionUnregister(System.Guid,int).EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister\.TournamentActionUnregister\(System\.Guid, int\)\.EntryNo') that is not
registered fails with [InvalidEntry](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidEntry 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidEntry')\. The entry leaves
[Entries](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.Entries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.Entries') and its buy\-in leaves the [PrizePool](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.PrizePool 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.PrizePool')\.

```csharp
public TournamentActionUnregister(System.Guid TournamentId, int EntryNo);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.TournamentActionUnregister(System.Guid,int).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to withdraw from\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.TournamentActionUnregister(System.Guid,int).EntryNo'></a>

`EntryNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo') to withdraw\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.EntryNo'></a>

## TournamentActionUnregister\.EntryNo Property

The [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo') to withdraw\.

```csharp
public int EntryNo { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
