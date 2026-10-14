#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemSchemaStatus Class

What [GetSchemaStatusAsync\(CancellationToken\)](TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.GetSchemaStatusAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.GetSchemaStatusAsync\(System\.Threading\.CancellationToken\)') found\. Two statuses read from the
same database compare equal, [PendingMigrations](TabletopCardEngine.Holdem.HoldemSchemaStatus.md#TabletopCardEngine.Holdem.HoldemSchemaStatus.PendingMigrations 'TabletopCardEngine\.Holdem\.HoldemSchemaStatus\.PendingMigrations') included, element by element\.

```csharp
public sealed record HoldemSchemaStatus : System.IEquatable<TabletopCardEngine.Holdem.HoldemSchemaStatus>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemSchemaStatus

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemSchemaStatus](TabletopCardEngine.Holdem.HoldemSchemaStatus.md 'TabletopCardEngine\.Holdem\.HoldemSchemaStatus')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_)'></a>

## HoldemSchemaStatus\(HoldemSchemaState, string, int, Nullable&lt;int&gt;, string, IReadOnlyList&lt;string&gt;\) Constructor

What [GetSchemaStatusAsync\(CancellationToken\)](TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.GetSchemaStatusAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.GetSchemaStatusAsync\(System\.Threading\.CancellationToken\)') found\. Two statuses read from the
same database compare equal, [PendingMigrations](TabletopCardEngine.Holdem.HoldemSchemaStatus.md#TabletopCardEngine.Holdem.HoldemSchemaStatus.PendingMigrations 'TabletopCardEngine\.Holdem\.HoldemSchemaStatus\.PendingMigrations') included, element by element\.

```csharp
public HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState State, string ProviderName, int EngineSchemaVersion, System.Nullable<int> DatabaseSchemaVersion, string? DatabaseEngineVersion, System.Collections.Generic.IReadOnlyList<string> PendingMigrations);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).State'></a>

`State` [HoldemSchemaState](TabletopCardEngine.Holdem.HoldemSchemaState.md 'TabletopCardEngine\.Holdem\.HoldemSchemaState')

How the database's schema compares with the engine's\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).ProviderName'></a>

`ProviderName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The EF Core provider name, such as `Microsoft.EntityFrameworkCore.Sqlite`\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).EngineSchemaVersion'></a>

`EngineSchemaVersion` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The schema version this engine expects\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).DatabaseSchemaVersion'></a>

`DatabaseSchemaVersion` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The schema version recorded in the database, or `null` when none is recorded\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).DatabaseEngineVersion'></a>

`DatabaseEngineVersion` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The version of the engine that last migrated the database, or `null` when none is recorded\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.HoldemSchemaStatus(TabletopCardEngine.Holdem.HoldemSchemaState,string,int,System.Nullable_int_,string,System.Collections.Generic.IReadOnlyList_string_).PendingMigrations'></a>

`PendingMigrations` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

The migrations the database has not applied yet, in the order they would be applied; empty when there are none\.
### Properties

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.DatabaseEngineVersion'></a>

## HoldemSchemaStatus\.DatabaseEngineVersion Property

The version of the engine that last migrated the database, or `null` when none is recorded\.

```csharp
public string? DatabaseEngineVersion { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.DatabaseSchemaVersion'></a>

## HoldemSchemaStatus\.DatabaseSchemaVersion Property

The schema version recorded in the database, or `null` when none is recorded\.

```csharp
public System.Nullable<int> DatabaseSchemaVersion { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.EngineSchemaVersion'></a>

## HoldemSchemaStatus\.EngineSchemaVersion Property

The schema version this engine expects\.

```csharp
public int EngineSchemaVersion { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.PendingMigrations'></a>

## HoldemSchemaStatus\.PendingMigrations Property

The migrations the database has not applied yet, in the order they would be applied; empty when there are none\.

```csharp
public System.Collections.Generic.IReadOnlyList<string> PendingMigrations { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.ProviderName'></a>

## HoldemSchemaStatus\.ProviderName Property

The EF Core provider name, such as `Microsoft.EntityFrameworkCore.Sqlite`\.

```csharp
public string ProviderName { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.HoldemSchemaStatus.State'></a>

## HoldemSchemaStatus\.State Property

How the database's schema compares with the engine's\.

```csharp
public TabletopCardEngine.Holdem.HoldemSchemaState State { get; init; }
```

#### Property Value
[HoldemSchemaState](TabletopCardEngine.Holdem.HoldemSchemaState.md 'TabletopCardEngine\.Holdem\.HoldemSchemaState')
