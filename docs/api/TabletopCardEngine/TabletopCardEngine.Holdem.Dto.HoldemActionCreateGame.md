#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionCreateGame Class

Creates a game\. Fails if one with this identifier already exists\.

```csharp
public record HoldemActionCreateGame : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionCreateGame

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionCreateGame](TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionCreateGame')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.HoldemActionCreateGame(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData)'></a>

## HoldemActionCreateGame\(Guid, GameOptionData\) Constructor

Creates a game\. Fails if one with this identifier already exists\.

```csharp
public HoldemActionCreateGame(System.Guid GameId, TabletopCardEngine.Holdem.Dto.GameOptionData Options);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.HoldemActionCreateGame(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

Identifier for the new game\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.HoldemActionCreateGame(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData).Options'></a>

`Options` [GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')

The rules the game is created with\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.Options'></a>

## HoldemActionCreateGame\.Options Property

The rules the game is created with\.

```csharp
public TabletopCardEngine.Holdem.Dto.GameOptionData Options { get; init; }
```

#### Property Value
[GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')
