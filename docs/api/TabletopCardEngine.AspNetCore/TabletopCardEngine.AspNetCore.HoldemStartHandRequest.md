#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemStartHandRequest Class

Carries the optional parameters for a `POST /games/{gameId}/hands` request\.

```csharp
public sealed record HoldemStartHandRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemStartHandRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemStartHandRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemStartHandRequest](TabletopCardEngine.AspNetCore.HoldemStartHandRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemStartHandRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemStartHandRequest.JoinSeats'></a>

## HoldemStartHandRequest\.JoinSeats Property

The seats to fill before dealing, for players who sit down between hands\. Optional; defaults to an empty list to deal to the players already seated\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerJoinData> JoinSeats { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerJoinData](../TabletopCardEngine/TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')
