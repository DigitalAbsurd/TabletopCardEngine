#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemSchemaFailure Enum

Machine\-readable reason a [HoldemSchemaException](TabletopCardEngine.Holdem.HoldemSchemaException.md 'TabletopCardEngine\.Holdem\.HoldemSchemaException') was thrown\. The numeric values are
part of the contract: explicit, never renumbered and never reused\. There is deliberately no zero
member, so `default` is never a valid failure\.

```csharp
public enum HoldemSchemaFailure
```
### Fields

<a name='TabletopCardEngine.Holdem.HoldemSchemaFailure.UnsupportedProvider'></a>

`UnsupportedProvider` 1

The configured EF Core provider is not one of the four the engine supports\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound'></a>

`MigrationsNotFound` 2

The provider is relational but no migrations were found, because it was configured with the
plain EF Core call instead of the `UseHoldem*` call of the provider's engine package\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaFailure.DatabaseIsNewer'></a>

`DatabaseIsNewer` 3

The database was migrated by a newer engine than this one\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationRequired'></a>

`MigrationRequired` 4

The database's schema is behind the engine's and has to be migrated first\.
