#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemStartGameRequest Class

Carries the players for a `POST /games/{gameId}/start` request\.

```csharp
public sealed record HoldemStartGameRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemStartGameRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemStartGameRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemStartGameRequest](TabletopCardEngine.AspNetCore.HoldemStartGameRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemStartGameRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemStartGameRequest.JoinSeats'></a>

## HoldemStartGameRequest\.JoinSeats Property

The players joining the game, in any order\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerJoinData> JoinSeats { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerJoinData](../TabletopCardEngine/TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')
