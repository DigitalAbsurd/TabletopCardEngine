#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemCreateGameRequest Class

Carries the optional parameters for a `POST /games` request\.

```csharp
public sealed record HoldemCreateGameRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemCreateGameRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemCreateGameRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemCreateGameRequest](TabletopCardEngine.AspNetCore.HoldemCreateGameRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemCreateGameRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemCreateGameRequest.GameId'></a>

## HoldemCreateGameRequest\.GameId Property

The game ID to assign\. Optional; the server generates a GUID when absent\.

```csharp
public System.Nullable<System.Guid> GameId { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.AspNetCore.HoldemCreateGameRequest.Options'></a>

## HoldemCreateGameRequest\.Options Property

The game options\. Optional; defaults to 6\-seat, 1/2 blinds, no\-limit hold'em\.

```csharp
public TabletopCardEngine.Holdem.Dto.GameOptionData Options { get; init; }
```

#### Property Value
[GameOptionData](../TabletopCardEngine/TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')
