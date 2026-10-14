#### [TabletopCardEngine](index.md 'index')

## TabletopCardEngine Namespace

| Classes | |
| :--- | :--- |
| [Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card') | One playing card\. Its string form is rank then suit, lowercase — see [ToString\(\)](TabletopCardEngine.Card.md#TabletopCardEngine.Card.ToString() 'TabletopCardEngine\.Card\.ToString\(\)')\. |
| [TabletopCardEngineBuilder](TabletopCardEngine.TabletopCardEngineBuilder.md 'TabletopCardEngine\.TabletopCardEngineBuilder') | The engine's registration, handed to the delegate of [AddTabletopCardEngine\(this IServiceCollection, Action&lt;TabletopCardEngineBuilder&gt;\)](TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.md#TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.AddTabletopCardEngine(thisMicrosoft.Extensions.DependencyInjection.IServiceCollection,System.Action_TabletopCardEngine.TabletopCardEngineBuilder_) 'TabletopCardEngine\.TabletopCardEngineServiceCollectionExtensions\.AddTabletopCardEngine\(this Microsoft\.Extensions\.DependencyInjection\.IServiceCollection, System\.Action&lt;TabletopCardEngine\.TabletopCardEngineBuilder&gt;\)')\.   It knows no game. Game packages extend it with `AddXxx` extension methods from their own namespace, such as `AddHoldem` in `TabletopCardEngine.Holdem`, which register their services into [Services](TabletopCardEngine.TabletopCardEngineBuilder.md#TabletopCardEngine.TabletopCardEngineBuilder.Services 'TabletopCardEngine\.TabletopCardEngineBuilder\.Services'). |
| [TabletopCardEngineServiceCollectionExtensions](TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.md 'TabletopCardEngine\.TabletopCardEngineServiceCollectionExtensions') | Registers the engine with a dependency\-injection container\. |
| [TabletopCardEngineTelemetry](TabletopCardEngine.TabletopCardEngineTelemetry.md 'TabletopCardEngine\.TabletopCardEngineTelemetry') | The telemetry sources declared by the Holdem engine for OpenTelemetry hosts\. |

| Enums | |
| :--- | :--- |
| [CardSuit](TabletopCardEngine.CardSuit.md 'TabletopCardEngine\.CardSuit') | A card's suit\. No suit outranks another; the description is the display glyph\. |
| [CardType](TabletopCardEngine.CardType.md 'TabletopCardEngine\.CardType') | A card's rank\. The numeric value is the pip count for 2\-10; the ace is `1` here but ranks above the king when hands are compared\. |
