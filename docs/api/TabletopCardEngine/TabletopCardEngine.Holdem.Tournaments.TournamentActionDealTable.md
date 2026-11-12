#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentActionDealTable Class

The host's call between two hands of one table of a running tournament: nothing deals a table's next hand by
itself\. It fails with [TournamentNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentNotStarted') while the tournament is registering, with
[TournamentFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentFinished') once it has finished, with [InvalidTable](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTable 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidTable')
for a table number the tournament does not have or a table that has been broken up, and with
[PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while the table's hand is still being played\.

Every player at the table who ended its last hand without chips is eliminated. With `P` players left before
the call and `b` of them out now, they take places `P - b + 1` to `P`, the bigger stack at the start
of that hand taking the better place. Players who started that hand with equal stacks tie: each gets the best
place of the group, and they share the prizes of the places the group covers equally, to the cent, any cent left
over going to the lower [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo'). The prize of place `k` is
[PrizePool](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.PrizePool 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.PrizePool') times the `k`-th of [Payouts](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Payouts 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.Payouts'), divided
by 100 and rounded to the cent, and `0` past the last payout; first place gets what the other paid places
leave of the pool, so the prizes add up to it exactly. An eliminated player's seat is freed.

When one player is left, that player takes first place and its prize, the tournament becomes
[Finished](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Finished 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Finished'), and every table's game is finished, after which `Play` on it fails with
[GameFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameFinished'). Otherwise, when the table still has at least two players, its next hand
is dealt at the level the tournament's clock shows now; a hand already in play keeps the level it was dealt at. A
table left with fewer than two players deals nothing and waits; the call still succeeds.

The call saves the tournament and that table's game together, so two tables never wait for each other; a lost race
with another call on the same tournament fails with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict').

```csharp
public record TournamentActionDealTable : TabletopCardEngine.Holdem.Tournaments.TournamentAction, System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction') → TournamentActionDealTable

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentActionDealTable](TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionDealTable')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable.TournamentActionDealTable(System.Guid,int)'></a>

## TournamentActionDealTable\(Guid, int\) Constructor

The host's call between two hands of one table of a running tournament: nothing deals a table's next hand by
itself\. It fails with [TournamentNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentNotStarted') while the tournament is registering, with
[TournamentFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TournamentFinished') once it has finished, with [InvalidTable](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTable 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidTable')
for a table number the tournament does not have or a table that has been broken up, and with
[PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while the table's hand is still being played\.

Every player at the table who ended its last hand without chips is eliminated. With `P` players left before
the call and `b` of them out now, they take places `P - b + 1` to `P`, the bigger stack at the start
of that hand taking the better place. Players who started that hand with equal stacks tie: each gets the best
place of the group, and they share the prizes of the places the group covers equally, to the cent, any cent left
over going to the lower [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo'). The prize of place `k` is
[PrizePool](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.PrizePool 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.PrizePool') times the `k`-th of [Payouts](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md#TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.Payouts 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData\.Payouts'), divided
by 100 and rounded to the cent, and `0` past the last payout; first place gets what the other paid places
leave of the pool, so the prizes add up to it exactly. An eliminated player's seat is freed.

When one player is left, that player takes first place and its prize, the tournament becomes
[Finished](TabletopCardEngine.Holdem.Tournaments.TournamentState.md#TabletopCardEngine.Holdem.Tournaments.TournamentState.Finished 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentState\.Finished'), and every table's game is finished, after which `Play` on it fails with
[GameFinished](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameFinished 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameFinished'). Otherwise, when the table still has at least two players, its next hand
is dealt at the level the tournament's clock shows now; a hand already in play keeps the level it was dealt at. A
table left with fewer than two players deals nothing and waits; the call still succeeds.

The call saves the tournament and that table's game together, so two tables never wait for each other; a lost race
with another call on the same tournament fails with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict').

```csharp
public TournamentActionDealTable(System.Guid TournamentId, int TableNo);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable.TournamentActionDealTable(System.Guid,int).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament the table belongs to\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable.TournamentActionDealTable(System.Guid,int).TableNo'></a>

`TableNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The [TableNo](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md#TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TableNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData\.TableNo') of the table to deal\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentActionDealTable.TableNo'></a>

## TournamentActionDealTable\.TableNo Property

The [TableNo](TabletopCardEngine.Holdem.Tournaments.TournamentTableData.md#TabletopCardEngine.Holdem.Tournaments.TournamentTableData.TableNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentTableData\.TableNo') of the table to deal\.

```csharp
public int TableNo { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
