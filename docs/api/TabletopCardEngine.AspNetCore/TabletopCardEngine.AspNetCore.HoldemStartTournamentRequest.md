#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemStartTournamentRequest Class

Carries the optional parameters for a `POST /tournaments/{tournamentId}/start` request\.

```csharp
public sealed record HoldemStartTournamentRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemStartTournamentRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemStartTournamentRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemStartTournamentRequest](TabletopCardEngine.AspNetCore.HoldemStartTournamentRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemStartTournamentRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemStartTournamentRequest.SeatDrawSeed'></a>

## HoldemStartTournamentRequest\.SeatDrawSeed Property

The seed of the seat draw, to reproduce one\. Optional; a seed is drawn at random when absent\.

```csharp
public System.Nullable<int> SeatDrawSeed { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')
