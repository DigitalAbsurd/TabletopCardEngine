#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionFinishGame Class

Closes the game, between hands only: it fails with [PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded')
while a hand is in progress\. Afterwards every action except [HoldemActionGetGame](TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionGetGame') fails with
[GameFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameFinished')\.

```csharp
public record HoldemActionFinishGame : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionFinishGame

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionFinishGame](TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionFinishGame')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame.HoldemActionFinishGame(System.Guid)'></a>

## HoldemActionFinishGame\(Guid\) Constructor

Closes the game, between hands only: it fails with [PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded')
while a hand is in progress\. Afterwards every action except [HoldemActionGetGame](TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionGetGame') fails with
[GameFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameFinished')\.

```csharp
public HoldemActionFinishGame(System.Guid GameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame.HoldemActionFinishGame(System.Guid).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to finish\.
