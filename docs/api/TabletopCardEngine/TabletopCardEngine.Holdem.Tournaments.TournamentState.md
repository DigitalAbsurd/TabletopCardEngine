#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentState Enum

Where a tournament is in its life\. A tournament only moves forward through these states\.

```csharp
public enum TournamentState : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentState.Registering'></a>

`Registering` 1

Players can register and unregister; no table exists yet\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentState.Running'></a>

`Running` 2

Registration is closed and the tables are playing\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentState.Finished'></a>

`Finished` 3

One player is left; the places and prizes are final\.
