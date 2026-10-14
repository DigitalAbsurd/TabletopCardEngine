#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemHostingBuilderExtensions Class

Registers Hold'em in an ASP\.NET Core host, with its settings bound from configuration\.

```csharp
public static class HoldemHostingBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemHostingBuilderExtensions
### Methods

<a name='TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_)'></a>

## HoldemHostingBuilderExtensions\.AddHoldem\(this TabletopCardEngineBuilder, IConfiguration, Action&lt;DbContextOptionsBuilder&gt;, Action&lt;HoldemOptionsBuilder&gt;\) Method

Registers Hold'em like the core `tce.AddHoldem(dbOptionsAction, configure)`, and takes its settings
from a configuration section bound to [HoldemHostOptions](TabletopCardEngine.AspNetCore.HoldemHostOptions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostOptions'), such as `TabletopCardEngine:Holdem`\.

The database stays in code: the package references no provider, so the host passes
[dbOptionsAction](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md#TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).dbOptionsAction 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions\.AddHoldem\(this TabletopCardEngine\.TabletopCardEngineBuilder, Microsoft\.Extensions\.Configuration\.IConfiguration, System\.Action&lt;Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder&gt;, System\.Action&lt;TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder&gt;\)\.dbOptionsAction') as before.

Precedence: the configuration section is the source of the settings, and it overwrites a
`WithSettings` call made inside [configure](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md#TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).configure 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions\.AddHoldem\(this TabletopCardEngine\.TabletopCardEngineBuilder, Microsoft\.Extensions\.Configuration\.IConfiguration, System\.Action&lt;Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder&gt;, System\.Action&lt;TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder&gt;\)\.configure'). Use [configure](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md#TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).configure 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions\.AddHoldem\(this TabletopCardEngine\.TabletopCardEngineBuilder, Microsoft\.Extensions\.Configuration\.IConfiguration, System\.Action&lt;Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder&gt;, System\.Action&lt;TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder&gt;\)\.configure')
for `WithCardShuffler`.

The section is validated with data annotations when the host starts, so a bad value fails
`StartAsync` with an [Microsoft\.Extensions\.Options\.OptionsValidationException](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.optionsvalidationexception 'Microsoft\.Extensions\.Options\.OptionsValidationException') before any request.

```csharp
public static TabletopCardEngine.TabletopCardEngineBuilder AddHoldem(this TabletopCardEngine.TabletopCardEngineBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration, System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> dbOptionsAction, System.Action<TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder>? configure=null);
```
#### Parameters

<a name='TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).builder'></a>

`builder` [TabletopCardEngineBuilder](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder')

The engine's registration\.

<a name='TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).configuration'></a>

`configuration` [Microsoft\.Extensions\.Configuration\.IConfiguration](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration 'Microsoft\.Extensions\.Configuration\.IConfiguration')

The section holding the [HoldemHostOptions](TabletopCardEngine.AspNetCore.HoldemHostOptions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostOptions') keys\.

<a name='TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).dbOptionsAction'></a>

`dbOptionsAction` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Configures the database provider, e\.g\. `db => db.UseHoldemSqlite(...)`\.

<a name='TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.AddHoldem(thisTabletopCardEngine.TabletopCardEngineBuilder,Microsoft.Extensions.Configuration.IConfiguration,System.Action_Microsoft.EntityFrameworkCore.DbContextOptionsBuilder_,System.Action_TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder_).configure'></a>

`configure` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[HoldemOptionsBuilder](../TabletopCardEngine/TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.md 'TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Configures the optional card shuffler; its settings are overwritten by the section\.

#### Returns
[TabletopCardEngineBuilder](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
Hold'em is already registered in this collection\.
