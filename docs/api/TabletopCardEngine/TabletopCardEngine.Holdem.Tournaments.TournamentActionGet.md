#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionGet Class

Reads the tournament back without changing anything; its version stays the same\.

```csharp
public record TournamentActionGet : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionGet>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionGet

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionGet](TabletopCardEngine.Holdem.Tournaments.TournamentActionGet.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionGet')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionGet.TournamentActionGet(System.Guid)'></a>

## TournamentActionGet\(Guid\) Constructor

Reads the tournament back without changing anything; its version stays the same\.

```csharp
public TournamentActionGet(System.Guid TournamentId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionGet.TournamentActionGet(System.Guid).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to read\.
