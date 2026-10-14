#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemPlayRequest Class

Carries the action for a `POST /games/{gameId}/actions` request\.

```csharp
public sealed record HoldemPlayRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemPlayRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemPlayRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemPlayRequest](TabletopCardEngine.AspNetCore.HoldemPlayRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemPlayRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.AspNetCore.HoldemPlayRequest.HoldemPlayRequest(TabletopCardEngine.Holdem.PlayerActionType)'></a>

## HoldemPlayRequest\(PlayerActionType\) Constructor

Carries the action for a `POST /games/{gameId}/actions` request\.

```csharp
public HoldemPlayRequest(TabletopCardEngine.Holdem.PlayerActionType ActionType);
```
#### Parameters

<a name='TabletopCardEngine.AspNetCore.HoldemPlayRequest.HoldemPlayRequest(TabletopCardEngine.Holdem.PlayerActionType).ActionType'></a>

`ActionType` [PlayerActionType](../TabletopCardEngine/TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

What the seat does\.
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemPlayRequest.ActionType'></a>

## HoldemPlayRequest\.ActionType Property

What the seat does\.

```csharp
public TabletopCardEngine.Holdem.PlayerActionType ActionType { get; init; }
```

#### Property Value
[PlayerActionType](../TabletopCardEngine/TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

<a name='TabletopCardEngine.AspNetCore.HoldemPlayRequest.Size'></a>

## HoldemPlayRequest\.Size Property

For a raise or a bet, the street total to raise or bet &lt;i&gt;to&lt;/i&gt;; ignored for the other actions\.

```csharp
public decimal Size { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
