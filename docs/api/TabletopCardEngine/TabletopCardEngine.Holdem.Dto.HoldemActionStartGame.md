#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionStartGame Class

Starts a created game: seats the players and deals hand 1\. Allowed once, on a game with no hand
dealt yet; otherwise it fails with [GameAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameAlreadyStarted')\.

```csharp
public record HoldemActionStartGame : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionStartGame>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionStartGame

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionStartGame](TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartGame')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.HoldemActionStartGame(System.Guid,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerJoinData_)'></a>

## HoldemActionStartGame\(Guid, IReadOnlyList&lt;PlayerJoinData&gt;\) Constructor

Starts a created game: seats the players and deals hand 1\. Allowed once, on a game with no hand
dealt yet; otherwise it fails with [GameAlreadyStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameAlreadyStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameAlreadyStarted')\.

```csharp
public HoldemActionStartGame(System.Guid GameId, System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerJoinData> JoinSeats);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.HoldemActionStartGame(System.Guid,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerJoinData_).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to start\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.HoldemActionStartGame(System.Guid,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerJoinData_).JoinSeats'></a>

`JoinSeats` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

The seats to fill, in any order; the engine sorts them by seat number\. Any read\-only list will do; the engine only reads it\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.JoinSeats'></a>

## HoldemActionStartGame\.JoinSeats Property

The seats to fill, in any order; the engine sorts them by seat number\. Any read\-only list will do; the engine only reads it\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerJoinData> JoinSeats { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')
