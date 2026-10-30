#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionLeaveSeat Class

Stands the player in a seat up, between hands only: it fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted')
before hand 1 and with [PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while a hand is in progress, so a
player who wants to quit mid\-hand folds first\. The seat's stack leaves with the player and is not dealt
into the next hand\. The seat is free for [JoinSeats](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md#TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.JoinSeats 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand\.JoinSeats') from the next hand on,
and until then [HasLeft](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.HasLeft 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.HasLeft') shows it\. A seat outside the table, an empty seat or a player
who has already left fails with [InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat')\. The engine never stands a seat up
on its own, not even a busted one\.

```csharp
public record HoldemActionLeaveSeat : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionLeaveSeat

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionLeaveSeat](TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionLeaveSeat')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.HoldemActionLeaveSeat(System.Guid,byte)'></a>

## HoldemActionLeaveSeat\(Guid, byte\) Constructor

Stands the player in a seat up, between hands only: it fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted')
before hand 1 and with [PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while a hand is in progress, so a
player who wants to quit mid\-hand folds first\. The seat's stack leaves with the player and is not dealt
into the next hand\. The seat is free for [JoinSeats](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md#TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.JoinSeats 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand\.JoinSeats') from the next hand on,
and until then [HasLeft](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.HasLeft 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.HasLeft') shows it\. A seat outside the table, an empty seat or a player
who has already left fails with [InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat')\. The engine never stands a seat up
on its own, not even a busted one\.

```csharp
public HoldemActionLeaveSeat(System.Guid GameId, byte SeatNo);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.HoldemActionLeaveSeat(System.Guid,byte).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to leave\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.HoldemActionLeaveSeat(System.Guid,byte).SeatNo'></a>

`SeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat whose player leaves\. Seats are 1\-based\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.SeatNo'></a>

## HoldemActionLeaveSeat\.SeatNo Property

The seat whose player leaves\. Seats are 1\-based\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')
