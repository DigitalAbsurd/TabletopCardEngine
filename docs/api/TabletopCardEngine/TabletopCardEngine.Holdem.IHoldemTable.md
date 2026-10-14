#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## IHoldemTable Interface

A Hold'em table: the single entry point for game commands, and every game action goes through [ApplyAsync\(HoldemAction, CancellationToken\)](TabletopCardEngine.Holdem.IHoldemTable.md#TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemTable\.ApplyAsync\(TabletopCardEngine\.Holdem\.Dto\.HoldemAction, System\.Threading\.CancellationToken\)')\.
There is no long\-lived in\-memory game: every call rehydrates the aggregate from the database, applies one action,
and projects the result back out\.

<b>Scope of this release:</b> No-Limit Texas Hold'em cash games at 2 to 6 seats, with no antes and no rake.
            Other betting structures, more seats, antes and tournaments are planned as later minor releases (see `docs/roadmap.md`),
            and [GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData') is where they will plug in.

The table is obtained from the dependency-injection container registered by
`AddHoldem`: `services.GetRequiredService<IHoldemTable>()`. It is an interface so that a
consumer's tests can substitute it.

```csharp
public interface IHoldemTable
```
### Methods

<a name='TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken)'></a>

## IHoldemTable\.ApplyAsync\(HoldemAction, CancellationToken\) Method

Applies one action to a game and returns the game as it now stands\.

A failed result short-circuits before the transaction is saved, so a validation failure
leaves nothing persisted.

<b>Concurrency:</b>[Version](TabletopCardEngine.Holdem.Dto.GameData.md#TabletopCardEngine.Holdem.Dto.GameData.Version 'TabletopCardEngine\.Holdem\.Dto\.GameData\.Version') counts the saves of a game, starting at `1`
            on creation; every call that saves increments it, and reading the game does not. The call fails with
            [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict') when [ExpectedVersion](TabletopCardEngine.Holdem.Dto.HoldemAction.md#TabletopCardEngine.Holdem.Dto.HoldemAction.ExpectedVersion 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction\.ExpectedVersion') is set and no
            longer matches, or when another writer saved the game between this call's load and its save. Nothing
            is persisted on a conflict, and the engine never retries: the caller reloads with
            [GetGame\(Guid\)](TabletopCardEngine.Holdem.Dto.HoldemAction.md#TabletopCardEngine.Holdem.Dto.HoldemAction.GetGame(System.Guid) 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction\.GetGame\(System\.Guid\)') and decides.

<b>Schema check:</b> the first call on each table instance checks the database schema version before it
            does anything else, and a failed check is not cached, so the next call checks again.

```csharp
System.Threading.Tasks.Task<TabletopCardEngine.Holdem.HoldemResult<TabletopCardEngine.Holdem.Dto.GameData>> ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction action, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken).action'></a>

`action` [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction')

The action to apply\. Its concrete record type decides which handler runs\.

<a name='TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Cancels the database work\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[TabletopCardEngine\.Holdem\.HoldemResult&lt;](TabletopCardEngine.Holdem.HoldemResult_T_.md 'TabletopCardEngine\.Holdem\.HoldemResult&lt;T&gt;')[GameData](TabletopCardEngine.Holdem.Dto.GameData.md 'TabletopCardEngine\.Holdem\.Dto\.GameData')[&gt;](TabletopCardEngine.Holdem.HoldemResult_T_.md 'TabletopCardEngine\.Holdem\.HoldemResult&lt;T&gt;')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The projected game on success, or a failed result carrying the validation errors, each with a stable [HoldemErrorCode](TabletopCardEngine.Holdem.HoldemErrorCode.md 'TabletopCardEngine\.Holdem\.HoldemErrorCode')\.

#### Exceptions

[HoldemSchemaException](TabletopCardEngine.Holdem.HoldemSchemaException.md 'TabletopCardEngine\.Holdem\.HoldemSchemaException')  
On the first call, when the database is newer than the engine, needs a migration, or is not versioned\.

[System\.OperationCanceledException](https://learn.microsoft.com/en-us/dotnet/api/system.operationcanceledexception 'System\.OperationCanceledException')  
When [cancellationToken](TabletopCardEngine.Holdem.IHoldemTable.md#TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken).cancellationToken 'TabletopCardEngine\.Holdem\.IHoldemTable\.ApplyAsync\(TabletopCardEngine\.Holdem\.Dto\.HoldemAction, System\.Threading\.CancellationToken\)\.cancellationToken') is cancelled\.

[Microsoft\.EntityFrameworkCore\.DbUpdateException](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbupdateexception 'Microsoft\.EntityFrameworkCore\.DbUpdateException')  
When the save fails for any reason other than a concurrency conflict; that is a fault, not a result\.
