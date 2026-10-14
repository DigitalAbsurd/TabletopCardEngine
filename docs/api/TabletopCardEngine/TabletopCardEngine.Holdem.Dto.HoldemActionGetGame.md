#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionGetGame Class

Reads the game back without changing anything\.

```csharp
public record HoldemActionGetGame : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionGetGame>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionGetGame

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionGetGame](TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionGetGame')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.HoldemActionGetGame(System.Guid)'></a>

## HoldemActionGetGame\(Guid\) Constructor

Reads the game back without changing anything\.

```csharp
public HoldemActionGetGame(System.Guid GameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.HoldemActionGetGame(System.Guid).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to read\.
