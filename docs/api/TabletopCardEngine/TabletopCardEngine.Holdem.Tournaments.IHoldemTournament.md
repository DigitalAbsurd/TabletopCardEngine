#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## IHoldemTournament Interface

The entry point for Hold'em tournaments, next to [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable')\. A tournament is an orchestrator of
ordinary Hold'em games: its tables are games the players act on through [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable'), while this
interface creates the tournament and takes the registrations\.

Like [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable'), it keeps no state in memory: every call reloads the tournament from the database,
applies the action and saves it. Resolve it from the container `AddHoldem` was called on.

```csharp
public interface IHoldemTournament
```
### Methods

<a name='TabletopCardEngine.Holdem.Tournaments.IHoldemTournament.ApplyAsync(TabletopCardEngine.Holdem.Tournaments.TournamentAction,System.Threading.CancellationToken)'></a>

## IHoldemTournament\.ApplyAsync\(TournamentAction, CancellationToken\) Method

Applies one action to a tournament and returns the tournament as it is afterwards\.

```csharp
System.Threading.Tasks.Task<TabletopCardEngine.Holdem.HoldemResult<TabletopCardEngine.Holdem.Tournaments.TournamentData>> ApplyAsync(TabletopCardEngine.Holdem.Tournaments.TournamentAction action, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.IHoldemTournament.ApplyAsync(TabletopCardEngine.Holdem.Tournaments.TournamentAction,System.Threading.CancellationToken).action'></a>

`action` [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction')

What to do\. Every action but [TournamentActionCreate](TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionCreate') needs a tournament that exists, and fails with
[TournamentNotFound](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentNotFound 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentNotFound') otherwise\.

<a name='TabletopCardEngine.Holdem.Tournaments.IHoldemTournament.ApplyAsync(TabletopCardEngine.Holdem.Tournaments.TournamentAction,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Cancels the database work\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[TabletopCardEngine\.Holdem\.HoldemResult&lt;](TabletopCardEngine.Holdem.HoldemResult_T_.md 'TabletopCardEngine\.Holdem\.HoldemResult&lt;T&gt;')[TournamentData](TabletopCardEngine.Holdem.Tournaments.TournamentData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData')[&gt;](TabletopCardEngine.Holdem.HoldemResult_T_.md 'TabletopCardEngine\.Holdem\.HoldemResult&lt;T&gt;')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The tournament on success\. On failure, the reason as a [HoldemError](TabletopCardEngine.Holdem.HoldemError.md 'TabletopCardEngine\.Holdem\.HoldemError'); nothing was saved then\.
