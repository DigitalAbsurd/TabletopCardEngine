#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentEntryState Enum

Where one registered player is in a tournament\.

```csharp
public enum TournamentEntryState : System.Byte
```
### Fields

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.Registered'></a>

`Registered` 1

Registered, and not seated yet\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.Seated'></a>

`Seated` 2

Playing at a table: [TableNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TableNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.TableNo') and [SeatNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.SeatNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.SeatNo') say where\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.Moving'></a>

`Moving` 3

Taken off a table that broke or was balanced, and waiting to be seated at another one with the same stack\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.Eliminated'></a>

`Eliminated` 4

Out of the tournament: [Place](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.Place 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.Place') and [Prize](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.Prize 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.Prize') are final\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.Winner'></a>

`Winner` 5

The last player left, in first place\.
