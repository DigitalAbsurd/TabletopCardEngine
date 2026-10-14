#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## PlayerActionData Class

A single action a seat has taken, as recorded in the hand's ledger\.

```csharp
public record PlayerActionData : System.IEquatable<TabletopCardEngine.Holdem.Dto.PlayerActionData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → PlayerActionData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[PlayerActionData](TabletopCardEngine.Holdem.Dto.PlayerActionData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.PlayerActionData(TabletopCardEngine.Holdem.Street,TabletopCardEngine.Holdem.HandActionType,decimal,int)'></a>

## PlayerActionData\(Street, HandActionType, decimal, int\) Constructor

A single action a seat has taken, as recorded in the hand's ledger\.

```csharp
public PlayerActionData(TabletopCardEngine.Holdem.Street Street, TabletopCardEngine.Holdem.HandActionType ActionType, decimal Size, int No);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.PlayerActionData(TabletopCardEngine.Holdem.Street,TabletopCardEngine.Holdem.HandActionType,decimal,int).Street'></a>

`Street` [Street](TabletopCardEngine.Holdem.Street.md 'TabletopCardEngine\.Holdem\.Street')

The street the action was taken on\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.PlayerActionData(TabletopCardEngine.Holdem.Street,TabletopCardEngine.Holdem.HandActionType,decimal,int).ActionType'></a>

`ActionType` [HandActionType](TabletopCardEngine.Holdem.HandActionType.md 'TabletopCardEngine\.Holdem\.HandActionType')

What the seat did\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.PlayerActionData(TabletopCardEngine.Holdem.Street,TabletopCardEngine.Holdem.HandActionType,decimal,int).Size'></a>

`Size` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

The size the caller asked for; `0` for actions that carry no size\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.PlayerActionData(TabletopCardEngine.Holdem.Street,TabletopCardEngine.Holdem.HandActionType,decimal,int).No'></a>

`No` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

Position of this action in the hand, ascending from 1\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.ActionType'></a>

## PlayerActionData\.ActionType Property

What the seat did\.

```csharp
public TabletopCardEngine.Holdem.HandActionType ActionType { get; init; }
```

#### Property Value
[HandActionType](TabletopCardEngine.Holdem.HandActionType.md 'TabletopCardEngine\.Holdem\.HandActionType')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.Amount'></a>

## PlayerActionData\.Amount Property

The seat's cumulative wager on this street after the action: what a blind actually posted, the total a call or check stands at, the street total a raise or bet went to; `0` for a fold\.

```csharp
public decimal Amount { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.No'></a>

## PlayerActionData\.No Property

Position of this action in the hand, ascending from 1\.

```csharp
public int No { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.SeatNo'></a>

## PlayerActionData\.SeatNo Property

The seat that took the action\. Seats are 1\-based\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.Size'></a>

## PlayerActionData\.Size Property

The size the caller asked for; `0` for actions that carry no size\.

```csharp
public decimal Size { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionData.Street'></a>

## PlayerActionData\.Street Property

The street the action was taken on\.

```csharp
public TabletopCardEngine.Holdem.Street Street { get; init; }
```

#### Property Value
[Street](TabletopCardEngine.Holdem.Street.md 'TabletopCardEngine\.Holdem\.Street')
