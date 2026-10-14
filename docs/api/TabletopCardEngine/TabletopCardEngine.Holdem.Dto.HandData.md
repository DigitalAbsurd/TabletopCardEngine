#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HandData Class

The current hand: who is in it, what is on the board, and what the seat to act may do\.

```csharp
public record HandData : System.IEquatable<TabletopCardEngine.Holdem.Dto.HandData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HandData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HandData](TabletopCardEngine.Holdem.Dto.HandData.md 'TabletopCardEngine\.Holdem\.Dto\.HandData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_)'></a>

## HandData\(int, IReadOnlyList&lt;SeatData&gt;, byte, byte, byte, byte, Street, decimal, decimal, IReadOnlyList&lt;Card&gt;, IReadOnlyList&lt;PlayerActionEvaluationData&gt;\) Constructor

The current hand: who is in it, what is on the board, and what the seat to act may do\.

```csharp
public HandData(int No, System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.SeatData> Seats, byte DealerSeatNo, byte SmallBlindSeatNo, byte BigBlindSeatNo, byte NextPlayerSeatNo, TabletopCardEngine.Holdem.Street Street, decimal StreetPot, decimal Pot, System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card> CommunityCards, System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData> Evaluations);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).No'></a>

`No` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

Hand number within the game, ascending from 1\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).Seats'></a>

`Seats` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[SeatData](TabletopCardEngine.Holdem.Dto.SeatData.md 'TabletopCardEngine\.Holdem\.Dto\.SeatData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

Every occupied seat, ascending by seat number\. Record equality compares it element by element for values the engine returns\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).DealerSeatNo'></a>

`DealerSeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

Where the button is\. Seats are 1\-based; `0` means "no dealer yet"\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).SmallBlindSeatNo'></a>

`SmallBlindSeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat that posted the small blind, or `0` before the hand starts\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).BigBlindSeatNo'></a>

`BigBlindSeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat that posted the big blind, or `0` before the hand starts\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).NextPlayerSeatNo'></a>

`NextPlayerSeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat to act, or `0` when it is nobody's turn\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).Street'></a>

`Street` [Street](TabletopCardEngine.Holdem.Street.md 'TabletopCardEngine\.Holdem\.Street')

The street now in progress\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).StreetPot'></a>

`StreetPot` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

Contributions made on the current street only\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).Pot'></a>

`Pot` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

Everything contributed on prior streets — [StreetPot](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).StreetPot 'TabletopCardEngine\.Holdem\.Dto\.HandData\.HandData\(int, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Holdem\.Dto\.SeatData&gt;, byte, byte, byte, byte, TabletopCardEngine\.Holdem\.Street, decimal, decimal, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Card&gt;, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Holdem\.Dto\.PlayerActionEvaluationData&gt;\)\.StreetPot') is not included\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).CommunityCards'></a>

`CommunityCards` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

The board, in dealt order\. Record equality compares it element by element for values the engine returns\.

<a name='TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).Evaluations'></a>

`Evaluations` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerActionEvaluationData](TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionEvaluationData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

What [NextPlayerSeatNo](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.HandData(int,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.SeatData_,byte,byte,byte,byte,TabletopCardEngine.Holdem.Street,decimal,decimal,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData_).NextPlayerSeatNo 'TabletopCardEngine\.Holdem\.Dto\.HandData\.HandData\(int, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Holdem\.Dto\.SeatData&gt;, byte, byte, byte, byte, TabletopCardEngine\.Holdem\.Street, decimal, decimal, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Card&gt;, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Holdem\.Dto\.PlayerActionEvaluationData&gt;\)\.NextPlayerSeatNo') may legally do; empty when it is nobody's turn\. Record equality compares it element by element for values the engine returns\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HandData.Actions'></a>

## HandData\.Actions Property

Every action of this hand in the order it was taken \(ascending `No`\), the engine's blind posts included\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerActionData> Actions { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerActionData](TabletopCardEngine.Holdem.Dto.PlayerActionData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.HandData.BigBlind'></a>

## HandData\.BigBlind Property

The nominal big blind of this hand\. A seat short of it posts what it has \(see [LastAction](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.LastAction 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.LastAction')\), so this is the level, not the amount posted\.

```csharp
public decimal BigBlind { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.HandData.BigBlindSeatNo'></a>

## HandData\.BigBlindSeatNo Property

The seat that posted the big blind, or `0` before the hand starts\.

```csharp
public byte BigBlindSeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.HandData.CommunityCards'></a>

## HandData\.CommunityCards Property

The board, in dealt order\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card> CommunityCards { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.HandData.DealerSeatNo'></a>

## HandData\.DealerSeatNo Property

Where the button is\. Seats are 1\-based; `0` means "no dealer yet"\.

```csharp
public byte DealerSeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.HandData.Evaluations'></a>

## HandData\.Evaluations Property

What NextPlayerSeatNo may legally do; empty when it is nobody's turn\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData> Evaluations { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[PlayerActionEvaluationData](TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionEvaluationData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.HandData.NextPlayerSeatNo'></a>

## HandData\.NextPlayerSeatNo Property

The seat to act, or `0` when it is nobody's turn\.

```csharp
public byte NextPlayerSeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.HandData.No'></a>

## HandData\.No Property

Hand number within the game, ascending from 1\.

```csharp
public int No { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Dto.HandData.Pot'></a>

## HandData\.Pot Property

Everything contributed on prior streets — StreetPot is not included\.

```csharp
public decimal Pot { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.HandData.Seats'></a>

## HandData\.Seats Property

Every occupied seat, ascending by seat number\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.Dto.SeatData> Seats { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[SeatData](TabletopCardEngine.Holdem.Dto.SeatData.md 'TabletopCardEngine\.Holdem\.Dto\.SeatData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.HandData.SmallBlind'></a>

## HandData\.SmallBlind Property

The nominal small blind of this hand\. Read the blinds here rather than deriving them from [GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData'): a later release may vary them from hand to hand\.

```csharp
public decimal SmallBlind { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.HandData.SmallBlindSeatNo'></a>

## HandData\.SmallBlindSeatNo Property

The seat that posted the small blind, or `0` before the hand starts\.

```csharp
public byte SmallBlindSeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.HandData.Street'></a>

## HandData\.Street Property

The street now in progress\.

```csharp
public TabletopCardEngine.Holdem.Street Street { get; init; }
```

#### Property Value
[Street](TabletopCardEngine.Holdem.Street.md 'TabletopCardEngine\.Holdem\.Street')

<a name='TabletopCardEngine.Holdem.Dto.HandData.StreetPot'></a>

## HandData\.StreetPot Property

Contributions made on the current street only\.

```csharp
public decimal StreetPot { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
### Methods

<a name='TabletopCardEngine.Holdem.Dto.HandData.GetLastAction()'></a>

## HandData\.GetLastAction\(\) Method

The most recent action taken by anyone in this hand\.

```csharp
public TabletopCardEngine.Holdem.Dto.PlayerActionData GetLastAction();
```

#### Returns
[PlayerActionData](TabletopCardEngine.Holdem.Dto.PlayerActionData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionData')
