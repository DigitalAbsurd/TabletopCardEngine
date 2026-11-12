#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionCreate Class

Creates a tournament in the [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') state, at version `1`\. Fails with
[TournamentAlreadyExists](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyExists 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyExists') if one with this identifier exists, with
[UnsupportedGameOptions](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.UnsupportedGameOptions 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.UnsupportedGameOptions') when the betting structure is not one the engine plays, and
with [InvalidTournamentOptions](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTournamentOptions 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidTournamentOptions') when an option is out of range \(see
[TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')\)\.

```csharp
public record TournamentActionCreate : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionCreate

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionCreate](TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionCreate')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.TournamentActionCreate(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData)'></a>

## TournamentActionCreate\(Guid, TournamentOptionData\) Constructor

Creates a tournament in the [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering') state, at version `1`\. Fails with
[TournamentAlreadyExists](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyExists 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyExists') if one with this identifier exists, with
[UnsupportedGameOptions](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.UnsupportedGameOptions 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.UnsupportedGameOptions') when the betting structure is not one the engine plays, and
with [InvalidTournamentOptions](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTournamentOptions 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidTournamentOptions') when an option is out of range \(see
[TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')\)\.

```csharp
public TournamentActionCreate(System.Guid TournamentId, TabletopCardEngine.Holdem.Tournaments.TournamentOptionData Options);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.TournamentActionCreate(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

Identifier for the new tournament\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.TournamentActionCreate(System.Guid,TabletopCardEngine.Holdem.Tournaments.TournamentOptionData).Options'></a>

`Options` [TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')

The rules the tournament is created with\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.Options'></a>

## TournamentActionCreate\.Options Property

The rules the tournament is created with\.

```csharp
public TabletopCardEngine.Holdem.Tournaments.TournamentOptionData Options { get; init; }
```

#### Property Value
[TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')
