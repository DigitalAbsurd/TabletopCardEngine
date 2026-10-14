#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## IHoldemSchemaManager Interface

The public way to create and inspect the engine's database schema, so that a host never has to
reach the engine's internal database context to do it\. `AddHoldem` registers one per
container, over the same database configuration as the engine itself\.

Exactly four providers are supported, recognised by their EF Core provider name: SQLite,
PostgreSQL (Npgsql), SQL Server and InMemory. Any other is rejected with
[UnsupportedProvider](TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.UnsupportedProvider 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.UnsupportedProvider').

```csharp
public interface IHoldemSchemaManager
```
### Methods

<a name='TabletopCardEngine.Holdem.IHoldemSchemaManager.GetSchemaStatusAsync(System.Threading.CancellationToken)'></a>

## IHoldemSchemaManager\.GetSchemaStatusAsync\(CancellationToken\) Method

Reports how the database's schema compares with the one this engine expects, without changing
anything\. For a supported provider it does not throw because the schema is behind; that is
what [MigrationRequired](TabletopCardEngine.Holdem.HoldemSchemaState.md#TabletopCardEngine.Holdem.HoldemSchemaState.MigrationRequired 'TabletopCardEngine\.Holdem\.HoldemSchemaState\.MigrationRequired') is for\.

```csharp
System.Threading.Tasks.Task<TabletopCardEngine.Holdem.HoldemSchemaStatus> GetSchemaStatusAsync(System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='TabletopCardEngine.Holdem.IHoldemSchemaManager.GetSchemaStatusAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Cancels the inspection\.

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[HoldemSchemaStatus](TabletopCardEngine.Holdem.HoldemSchemaStatus.md 'TabletopCardEngine\.Holdem\.HoldemSchemaStatus')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')  
The schema's current status\.

#### Exceptions

[HoldemSchemaException](TabletopCardEngine.Holdem.HoldemSchemaException.md 'TabletopCardEngine\.Holdem\.HoldemSchemaException')  
The provider is not supported \([UnsupportedProvider](TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.UnsupportedProvider 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.UnsupportedProvider')\)\.

<a name='TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken)'></a>

## IHoldemSchemaManager\.MigrateAsync\(CancellationToken\) Method

Brings the schema up to date\. On a relational database this applies the engine's migrations,
which come from the provider's `TabletopCardEngine.*` package; on InMemory it creates the
model, since there is nothing to migrate\.

```csharp
System.Threading.Tasks.Task MigrateAsync(System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Cancels the migration\.

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')  
A task that completes once the schema is up to date\.

#### Exceptions

[HoldemSchemaException](TabletopCardEngine.Holdem.HoldemSchemaException.md 'TabletopCardEngine\.Holdem\.HoldemSchemaException')  
The provider is not supported \([UnsupportedProvider](TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.UnsupportedProvider 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.UnsupportedProvider')\), or it is
relational but configured without the package that carries the migrations
\([MigrationsNotFound](TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\)\. A failure to reach the database is not
wrapped: it surfaces as the provider reports it\.
