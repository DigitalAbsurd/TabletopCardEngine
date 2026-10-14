#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionPlay Class

Plays one action for the seat whose turn it is\. An action [Evaluations](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.Evaluations 'TabletopCardEngine\.Holdem\.Dto\.HandData\.Evaluations') does not
offer, a raise or bet size outside its window, a bet while a wager is open, fails with [IllegalAction](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.IllegalAction 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.IllegalAction') and
changes nothing\.

```csharp
public record HoldemActionPlay : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionPlay>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionPlay

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionPlay](TabletopCardEngine.Holdem.Dto.HoldemActionPlay.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionPlay')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.HoldemActionPlay(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal)'></a>

## HoldemActionPlay\(Guid, PlayerActionType, decimal\) Constructor

Plays one action for the seat whose turn it is\. An action [Evaluations](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.Evaluations 'TabletopCardEngine\.Holdem\.Dto\.HandData\.Evaluations') does not
offer, a raise or bet size outside its window, a bet while a wager is open, fails with [IllegalAction](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.IllegalAction 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.IllegalAction') and
changes nothing\.

```csharp
public HoldemActionPlay(System.Guid GameId, TabletopCardEngine.Holdem.PlayerActionType ActionType, decimal Size);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.HoldemActionPlay(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to act in\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.HoldemActionPlay(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).ActionType'></a>

`ActionType` [PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

What the seat does\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.HoldemActionPlay(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).Size'></a>

`Size` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

For [Raise](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Raise 'TabletopCardEngine\.Holdem\.PlayerActionType\.Raise') or [Bet](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Bet 'TabletopCardEngine\.Holdem\.PlayerActionType\.Bet'), the street total to raise or bet &lt;i&gt;to&lt;/i&gt; — the same absolute
basis as the raise bounds in [Evaluations](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.Evaluations 'TabletopCardEngine\.Holdem\.Dto\.HandData\.Evaluations')\. Must have at most two decimal places\. Ignored for other actions\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.ActionType'></a>

## HoldemActionPlay\.ActionType Property

What the seat does\.

```csharp
public TabletopCardEngine.Holdem.PlayerActionType ActionType { get; init; }
```

#### Property Value
[PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionPlay.Size'></a>

## HoldemActionPlay\.Size Property

For [Raise](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Raise 'TabletopCardEngine\.Holdem\.PlayerActionType\.Raise') or [Bet](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Bet 'TabletopCardEngine\.Holdem\.PlayerActionType\.Bet'), the street total to raise or bet &lt;i&gt;to&lt;/i&gt; — the same absolute
basis as the raise bounds in [Evaluations](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.Evaluations 'TabletopCardEngine\.Holdem\.Dto\.HandData\.Evaluations')\. Must have at most two decimal places\. Ignored for other actions\.

```csharp
public decimal Size { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
