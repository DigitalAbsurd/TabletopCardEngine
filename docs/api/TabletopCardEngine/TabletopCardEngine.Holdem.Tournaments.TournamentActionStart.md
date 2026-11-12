#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionStart Class

Starts a tournament that is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering'); afterwards it fails with
[TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted'), and with fewer than
[MinEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MinEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MinEntries') entries it fails with [NotEnoughPlayers](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.NotEnoughPlayers 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.NotEnoughPlayers')\.

Registration closes and the seats are drawn at random: the entries are split into as few tables of
[MaxPlayersPerTable](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxPlayersPerTable 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MaxPlayersPerTable') seats as they fit in, whose sizes differ by at most one, each
seated from seat 1 up with a button drawn among its seats. Each table is an ordinary game, listed in
[Tables](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.Tables 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.Tables'), whose first hand is dealt at once at the first level with every player at
[StartingStack](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.StartingStack 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.StartingStack'); a table drawn with a single player waits without a hand. The
level clock starts now, and the prize pool is fixed.

Players act on their table through [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable') with its game id, which takes `Play` and
`GetGame` only: every other action on a tournament table fails with
[ManagedByTournament](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.ManagedByTournament 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.ManagedByTournament').

```csharp
public record TournamentActionStart : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionStart>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionStart

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionStart](TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionStart')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.TournamentActionStart(System.Guid)'></a>

## TournamentActionStart\(Guid\) Constructor

Starts a tournament that is [Registering](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Registering'); afterwards it fails with
[TournamentAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentAlreadyStarted'), and with fewer than
[MinEntries](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MinEntries 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MinEntries') entries it fails with [NotEnoughPlayers](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.NotEnoughPlayers 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.NotEnoughPlayers')\.

Registration closes and the seats are drawn at random: the entries are split into as few tables of
[MaxPlayersPerTable](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.MaxPlayersPerTable 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.MaxPlayersPerTable') seats as they fit in, whose sizes differ by at most one, each
seated from seat 1 up with a button drawn among its seats. Each table is an ordinary game, listed in
[Tables](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.Tables 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.Tables'), whose first hand is dealt at once at the first level with every player at
[StartingStack](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.StartingStack 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.StartingStack'); a table drawn with a single player waits without a hand. The
level clock starts now, and the prize pool is fixed.

Players act on their table through [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable') with its game id, which takes `Play` and
`GetGame` only: every other action on a tournament table fails with
[ManagedByTournament](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.ManagedByTournament 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.ManagedByTournament').

```csharp
public TournamentActionStart(System.Guid TournamentId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.TournamentActionStart(System.Guid).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to start\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.SeatDrawSeed'></a>

## TournamentActionStart\.SeatDrawSeed Property

The seed of the seat draw; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') \(the default\) draws a seed at random\. Either way the seed
is kept, so a start can be reproduced: one seed gives one draw of seats and buttons for the same entries\.

```csharp
public System.Nullable<int> SeatDrawSeed { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')
