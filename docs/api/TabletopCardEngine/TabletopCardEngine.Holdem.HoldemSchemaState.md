#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemSchemaState Enum

How a database's schema compares with the one the engine expects\. The numeric values are part of
the contract: explicit, never renumbered and never reused\. There is deliberately no zero member,
so `default` is never a valid state\.

```csharp
public enum HoldemSchemaState
```
### Fields

<a name='TabletopCardEngine.Holdem.HoldemSchemaState.UpToDate'></a>

`UpToDate` 1

Every migration the engine knows of has been applied\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaState.MigrationRequired'></a>

`MigrationRequired` 2

The engine has migrations the database has not applied yet; call [MigrateAsync\(CancellationToken\)](TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)')\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaState.NewerThanEngine'></a>

`NewerThanEngine` 3

The database was migrated by a newer engine than this one, which must not write to it\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaState.NotVersioned'></a>

`NotVersioned` 4

The provider keeps no schema history \(InMemory\), so there is no version to compare\.
