#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionStartHand Class

Deals the next hand, optionally seating players in empty seats first\. Fails with
[GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') until [HoldemActionStartGame](TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartGame') has dealt hand 1\.

```csharp
public record HoldemActionStartHand : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionStartHand>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionStartHand

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionStartHand](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.HoldemActionStartHand(System.Guid)'></a>

## HoldemActionStartHand\(Guid\) Constructor

Deals the next hand, optionally seating players in empty seats first\. Fails with
[GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') until [HoldemActionStartGame](TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartGame') has dealt hand 1\.

```csharp
public HoldemActionStartHand(System.Guid GameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.HoldemActionStartHand(System.Guid).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to deal in\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.JoinSeats'></a>

## HoldemActionStartHand\.JoinSeats Property

Players who sit down between hands, each into an empty seat: never occupied, or left with [HoldemActionLeaveSeat](TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionLeaveSeat'); topping up an occupied seat is not supported\. Any read\-only list will do; the engine only reads it\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerJoinData> JoinSeats { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')
