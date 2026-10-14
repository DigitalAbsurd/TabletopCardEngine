#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine](TabletopCardEngine.md 'TabletopCardEngine')

## TabletopCardEngineBuilder Class

The engine's registration, handed to the delegate of
[AddTabletopCardEngine\(this IServiceCollection, Action&lt;TabletopCardEngineBuilder&gt;\)](TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.md#TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_TabletopCardEngine.TabletopCardEngineBuilder_) 'TabletopCardEngine\.TabletopCardEngineServiceCollectionExtensions\.AddTabletopCardEngine\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Action&lt;TabletopCardEngine\.TabletopCardEngineBuilder&gt;\)')\.

It knows no game. Game packages extend it with `AddXxx` extension methods from their own
namespace, such as `AddHoldem` in `TabletopCardEngine.Holdem`, which register their
services into [Services](TabletopCardEngine.TabletopCardEngineBuilder.md#TabletopCardEngine.TabletopCardEngineBuilder.Services 'TabletopCardEngine\.TabletopCardEngineBuilder\.Services').

```csharp
public sealed class TabletopCardEngineBuilder
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TabletopCardEngineBuilder
### Properties

<a name='TabletopCardEngine.TabletopCardEngineBuilder.Services'></a>

## TabletopCardEngineBuilder\.Services Property

The container the games are registered into\.

```csharp
public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
```

#### Property Value
[Microsoft\.Extensions\.DependencyInjection\.IServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection 'Microsoft\.Extensions\.DependencyInjection\.IServiceCollection')
