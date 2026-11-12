#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionRegister Class

Registers a player, while the tournament is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') only; afterwards it fails
with [TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted')\. Once [MaxEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MaxEntries')
players are registered it fails with [TournamentFull](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFull 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentFull'), and a name that is
[null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null'), empty, whitespace only, longer than 64 characters, or already registered \(compared
ignoring case\) fails with [InvalidEntry](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidEntry 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidEntry')\. The new entry's
[EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo') is one more than the highest ever given in this tournament, so a
number is never given twice, not even after a [TournamentActionUnregister](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister')\.

```csharp
public record TournamentActionRegister : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionRegister

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionRegister](TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionRegister')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.TournamentActionRegister(System.Guid,string)'></a>

## TournamentActionRegister\(Guid, string\) Constructor

Registers a player, while the tournament is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') only; afterwards it fails
with [TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted')\. Once [MaxEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MaxEntries')
players are registered it fails with [TournamentFull](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFull 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentFull'), and a name that is
[null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null'), empty, whitespace only, longer than 64 characters, or already registered \(compared
ignoring case\) fails with [InvalidEntry](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidEntry 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidEntry')\. The new entry's
[EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo') is one more than the highest ever given in this tournament, so a
number is never given twice, not even after a [TournamentActionUnregister](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister')\.

```csharp
public TournamentActionRegister(System.Guid TournamentId, string Name);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.TournamentActionRegister(System.Guid,string).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to register for\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.TournamentActionRegister(System.Guid,string).Name'></a>

`Name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The player's display name\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.Name'></a>

## TournamentActionRegister\.Name Property

The player's display name\.

```csharp
public string Name { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
