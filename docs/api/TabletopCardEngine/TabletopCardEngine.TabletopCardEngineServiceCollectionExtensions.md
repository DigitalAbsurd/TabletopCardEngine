#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine](TabletopCardEngine.md 'TabletopCardEngine')

## TabletopCardEngineServiceCollectionExtensions Class

Registers the engine with a dependency\-injection container\.

```csharp
public static class TabletopCardEngineServiceCollectionExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TabletopCardEngineServiceCollectionExtensions
### Methods

<a name='TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_TabletopCardEngine.TabletopCardEngineBuilder_)'></a>

## TabletopCardEngineServiceCollectionExtensions\.AddTabletopCardEngine\(this IServiceCollection, Action&lt;TabletopCardEngineBuilder&gt;\) Method

Registers the engine, naming the engine first and each game second:
`services.AddTabletopCardEngine(tce => tce.AddHoldem(...))`\.

The call registers nothing on its own. Every game is added by its own `AddXxx` extension
method on [TabletopCardEngineBuilder](TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder'), so a container without one has no game service.

```csharp
public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTabletopCardEngine(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<TabletopCardEngine.TabletopCardEngineBuilder> configure);
```
#### Parameters

<a name='TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_TabletopCardEngine.TabletopCardEngineBuilder_).services'></a>

`services` [Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')

The container to register into\.

<a name='TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_TabletopCardEngine.TabletopCardEngineBuilder_).configure'></a>

`configure` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[TabletopCardEngineBuilder](TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Adds the games, e\.g\. `tce => tce.AddHoldem(db => db.UseHoldemSqlite(...))`\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')  
The same collection, for chaining\.
