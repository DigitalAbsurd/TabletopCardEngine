#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemCreateTournamentRequest Class

Carries the options for a `POST /tournaments` request\.

```csharp
public sealed record HoldemCreateTournamentRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemCreateTournamentRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemCreateTournamentRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemCreateTournamentRequest](TabletopCardEngine.AspNetCore.HoldemCreateTournamentRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemCreateTournamentRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemCreateTournamentRequest.Options'></a>

## HoldemCreateTournamentRequest\.Options Property

The tournament rules\. A level's `duration` is a `"hh:mm:ss"` string such as `"00:10:00"`\. The levels have no default, so the request has to set them\.

```csharp
public TabletopCardEngine.Holdem.Tournaments.TournamentOptionData Options { get; init; }
```

#### Property Value
[TournamentOptionData](../TabletopCardEngine/TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')

<a name='TabletopCardEngine.AspNetCore.HoldemCreateTournamentRequest.TournamentId'></a>

## HoldemCreateTournamentRequest\.TournamentId Property

The tournament ID to assign\. Optional; the server generates a GUID when absent\.

```csharp
public System.Nullable<System.Guid> TournamentId { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')
