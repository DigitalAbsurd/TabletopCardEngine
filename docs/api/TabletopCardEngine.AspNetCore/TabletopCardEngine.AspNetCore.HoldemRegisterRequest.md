#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemRegisterRequest Class

Carries the player for a `POST /tournaments/{tournamentId}/entries` request\.

```csharp
public sealed record HoldemRegisterRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemRegisterRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemRegisterRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemRegisterRequest](TabletopCardEngine.AspNetCore.HoldemRegisterRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemRegisterRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemRegisterRequest.Name'></a>

## HoldemRegisterRequest\.Name Property

The player's display name, 1 to 64 characters, unique within the tournament regardless of case\.

```csharp
public string Name { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
