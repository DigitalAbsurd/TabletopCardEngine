#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemBuilderExtensions Class

Adds Texas Hold'em to the engine's registration\.

```csharp
public static class HoldemBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemBuilderExtensions
### Methods

<a name='TabletopCardEngine.Holdem.HoldemBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_)'></a>

## HoldemBuilderExtensions\.AddHoldem\(this TabletopCardEngineBuilder, Action&lt;DbContextOptionsBuilder&gt;, Action&lt;HoldemOptionsBuilder&gt;\) Method

Registers [IHoldemTable](TabletopCardEngine.Holdem.IHoldemTable.md 'TabletopCardEngine\.Holdem\.IHoldemTable') as a singleton over an EF Core context factory\.

Engine options belong to the container: each call creates its own engine options and
registers them as a singleton instance, so engines built from different containers can be
configured differently side by side in one process. A container holds one Hold'em
registration, so a second call on the same collection throws, whether it is made through
the same builder or through a second `AddTabletopCardEngine` call.

The engine does not create its own tables. The schema is created with
[MigrateAsync\(CancellationToken\)](TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)'), which this call registers per container as
well, over the same database configuration.

```csharp
public static TabletopCardEngine.TabletopCardEngineBuilder AddHoldem(this TabletopCardEngine.TabletopCardEngineBuilder builder, System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> dbOptionsAction, System.Action<TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder>? configure=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.HoldemBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).builder'></a>

`builder` [TabletopCardEngineBuilder](TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder')

The engine's registration\.

<a name='TabletopCardEngine.Holdem.HoldemBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).dbOptionsAction'></a>

`dbOptionsAction` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Configures the database provider, e\.g\. `db => db.UseHoldemSqlite(...)`\.

<a name='TabletopCardEngine.Holdem.HoldemBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).configure'></a>

`configure` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[HoldemOptionsBuilder](TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.md 'TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Configures engine settings and the optional [ICardShuffler](TabletopCardEngine.Holdem.ICardShuffler.md 'TabletopCardEngine\.Holdem\.ICardShuffler')\.

#### Returns
[TabletopCardEngineBuilder](TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Hold'em is already registered in this collection\.
