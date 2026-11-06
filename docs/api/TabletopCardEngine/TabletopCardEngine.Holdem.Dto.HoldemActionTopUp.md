#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemActionTopUp Class

Adds chips to an occupied seat between hands\. The chips are not in play until the next
[HoldemActionStartHand](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand'), which deals the seat its finish stack plus every amount added since\.
What the call is depends on the stack the seat would start that hand with before it: with chips left it is a
top\-up, allowed by [AllowTopUp](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.AllowTopUp 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.AllowTopUp') \(otherwise [TopUpNotAllowed](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TopUpNotAllowed 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TopUpNotAllowed')\)
and only up to [MaxBuyIn](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.MaxBuyIn 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.MaxBuyIn') in total; with none left it is a rebuy, allowed by
[AllowRebuy](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.AllowRebuy 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.AllowRebuy') at most [MaxRebuys](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.MaxRebuys 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.MaxRebuys') times per player
\(otherwise [RebuyNotAllowed](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.RebuyNotAllowed 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.RebuyNotAllowed')\) and inside the buy\-in window\. An amount outside those
limits, not positive, or with more than two decimal places fails with [InvalidBuyIn](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidBuyIn 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidBuyIn')\.
It fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') before hand 1, with
[PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while a hand is in progress, and with
[InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat') for a seat outside the table, an empty seat or a player who has left\.
A player who leaves before the next hand takes back nothing: the pending chips are cancelled, as they are when the
game is finished\.

```csharp
public record HoldemActionTopUp : TabletopCardEngine.Holdem.Dto.HoldemAction, System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemActionTopUp>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction') → HoldemActionTopUp

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemActionTopUp](TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionTopUp')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.HoldemActionTopUp(System.Guid,byte,decimal)'></a>

## HoldemActionTopUp\(Guid, byte, decimal\) Constructor

Adds chips to an occupied seat between hands\. The chips are not in play until the next
[HoldemActionStartHand](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand'), which deals the seat its finish stack plus every amount added since\.
What the call is depends on the stack the seat would start that hand with before it: with chips left it is a
top\-up, allowed by [AllowTopUp](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.AllowTopUp 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.AllowTopUp') \(otherwise [TopUpNotAllowed](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.TopUpNotAllowed 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.TopUpNotAllowed')\)
and only up to [MaxBuyIn](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.MaxBuyIn 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.MaxBuyIn') in total; with none left it is a rebuy, allowed by
[AllowRebuy](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.AllowRebuy 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.AllowRebuy') at most [MaxRebuys](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.MaxRebuys 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.MaxRebuys') times per player
\(otherwise [RebuyNotAllowed](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.RebuyNotAllowed 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.RebuyNotAllowed')\) and inside the buy\-in window\. An amount outside those
limits, not positive, or with more than two decimal places fails with [InvalidBuyIn](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidBuyIn 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidBuyIn')\.
It fails with [GameNotStarted](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.GameNotStarted') before hand 1, with
[PreviousHandNotEnded](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.PreviousHandNotEnded') while a hand is in progress, and with
[InvalidSeat](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.InvalidSeat') for a seat outside the table, an empty seat or a player who has left\.
A player who leaves before the next hand takes back nothing: the pending chips are cancelled, as they are when the
game is finished\.

```csharp
public HoldemActionTopUp(System.Guid GameId, byte SeatNo, decimal Amount);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.HoldemActionTopUp(System.Guid,byte,decimal).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to add chips in\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.HoldemActionTopUp(System.Guid,byte,decimal).SeatNo'></a>

`SeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat whose player adds chips\. Seats are 1\-based\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.HoldemActionTopUp(System.Guid,byte,decimal).Amount'></a>

`Amount` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

The chips to add, positive and with at most two decimal places\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.Amount'></a>

## HoldemActionTopUp\.Amount Property

The chips to add, positive and with at most two decimal places\.

```csharp
public decimal Amount { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.HoldemActionTopUp.SeatNo'></a>

## HoldemActionTopUp\.SeatNo Property

The seat whose player adds chips\. Seats are 1\-based\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')
