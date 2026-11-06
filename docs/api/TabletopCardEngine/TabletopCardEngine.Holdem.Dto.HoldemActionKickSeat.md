#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionKickSeat Class

Removes the player in a seat, between hands or during one, for a host that has to take a disconnected or disruptive
player off the table without waiting for them\. The engine has no notion of who may send it: authorising the call is
the host application's job\.

Between hands it does exactly what [HoldemActionLeaveSeat](TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionLeaveSeat') does: the stack leaves with the player, a
pending top-up or rebuy is cancelled, and the seat is free for [JoinSeats](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md#TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.JoinSeats 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand\.JoinSeats') from the
next hand on.

During a hand the player leaves after it. A seat that has folded already, or is all in, keeps its place: an all-in
seat stays in the pot to showdown, and whatever it wins leaves with it. Any other seat folds at once, whether or not
it is the seat to act; the ledger records an ordinary [Fold](TabletopCardEngine.Holdem.HandActionType.md#TabletopCardEngine.Holdem.HandActionType.Fold 'TabletopCardEngine\.Holdem\.HandActionType\.Fold'). A seat folded out of turn
does not move the turn: [NextPlayerSeatNo](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.NextPlayerSeatNo 'TabletopCardEngine\.Holdem\.Dto\.HandData\.NextPlayerSeatNo') stays where it was, unless the fold leaves a single
seat contesting the pot, which then wins it and the hand ends as on any other fold.

[HasLeft](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.HasLeft 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.HasLeft') shows the departure. It fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') before
            hand 1, and with [InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat') for a seat outside the table, an empty seat, or a player who
            has already left or been removed.

```csharp
public record HoldemActionKickSeat : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionKickSeat

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionKickSeat](TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionKickSeat')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.HoldemActionKickSeat(System.Guid,byte)'></a>

## HoldemActionKickSeat\(Guid, byte\) Constructor

Removes the player in a seat, between hands or during one, for a host that has to take a disconnected or disruptive
player off the table without waiting for them\. The engine has no notion of who may send it: authorising the call is
the host application's job\.

Between hands it does exactly what [HoldemActionLeaveSeat](TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionLeaveSeat') does: the stack leaves with the player, a
pending top-up or rebuy is cancelled, and the seat is free for [JoinSeats](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md#TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.JoinSeats 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand\.JoinSeats') from the
next hand on.

During a hand the player leaves after it. A seat that has folded already, or is all in, keeps its place: an all-in
seat stays in the pot to showdown, and whatever it wins leaves with it. Any other seat folds at once, whether or not
it is the seat to act; the ledger records an ordinary [Fold](TabletopCardEngine.Holdem.HandActionType.md#TabletopCardEngine.Holdem.HandActionType.Fold 'TabletopCardEngine\.Holdem\.HandActionType\.Fold'). A seat folded out of turn
does not move the turn: [NextPlayerSeatNo](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.NextPlayerSeatNo 'TabletopCardEngine\.Holdem\.Dto\.HandData\.NextPlayerSeatNo') stays where it was, unless the fold leaves a single
seat contesting the pot, which then wins it and the hand ends as on any other fold.

[HasLeft](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.HasLeft 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.HasLeft') shows the departure. It fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') before
            hand 1, and with [InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat') for a seat outside the table, an empty seat, or a player who
            has already left or been removed.

```csharp
public HoldemActionKickSeat(System.Guid GameId, byte SeatNo);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.HoldemActionKickSeat(System.Guid,byte).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to remove the player from\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.HoldemActionKickSeat(System.Guid,byte).SeatNo'></a>

`SeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat whose player is removed\. Seats are 1\-based\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionKickSeat.SeatNo'></a>

## HoldemActionKickSeat\.SeatNo Property

The seat whose player is removed\. Seats are 1\-based\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')
