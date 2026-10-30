#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## SeatData Class

One seat as it stands in the current hand\.

```csharp
public record SeatData : System.IEquatable<TabletopCardEngine.Holdem.Dto.SeatData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → SeatData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[SeatData](TabletopCardEngine.Holdem.Dto.SeatData.md 'TabletopCardEngine\.Holdem\.Dto\.SeatData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData)'></a>

## SeatData\(byte, string, decimal, decimal, Nullable&lt;decimal&gt;, IReadOnlyList&lt;Card&gt;, PlayerActionData, HandResultData\) Constructor

One seat as it stands in the current hand\.

```csharp
public SeatData(byte SeatNo, string Name, decimal StartHandStack, decimal Stack, System.Nullable<decimal> BetSize=null, System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card>? Hole=null, TabletopCardEngine.Holdem.Dto.PlayerActionData? LastAction=null, TabletopCardEngine.Holdem.Dto.HandResultData? HandResult=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).SeatNo'></a>

`SeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat number\. Seats are 1\-based; `0` means "no seat"\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).Name'></a>

`Name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Display name of the player in the seat\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).StartHandStack'></a>

`StartHandStack` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

The stack this seat sat down with at the start of the hand\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).Stack'></a>

`Stack` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

The stack right now — [StartHandStack](TabletopCardEngine.Holdem.Dto.SeatData.md#TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).StartHandStack 'TabletopCardEngine\.Holdem\.Dto\.SeatData\.SeatData\(byte, string, decimal, decimal, System\.Nullable&lt;decimal&gt;, System\.Collections\.Generic\.IReadOnlyList&lt;TabletopCardEngine\.Card&gt;, TabletopCardEngine\.Holdem\.Dto\.PlayerActionData, TabletopCardEngine\.Holdem\.Dto\.HandResultData\)\.StartHandStack') minus everything wagered this hand\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).BetSize'></a>

`BetSize` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The seat's wager on the street now on the clock — its highest cumulative amount in the ledger for that street, the same street [StreetPot](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.StreetPot 'TabletopCardEngine\.Holdem\.Dto\.HandData\.StreetPot') counts; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when the seat has no action on that street yet \(a new street opens with everyone at [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\)\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).Hole'></a>

`Hole` [System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

The seat's hole cards, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when they are not visible to the caller\. Record equality compares it element by element for values the engine returns\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).LastAction'></a>

`LastAction` [PlayerActionData](TabletopCardEngine.Holdem.Dto.PlayerActionData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionData')

The most recent action this seat took, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if it has not acted\.

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatData(byte,string,decimal,decimal,System.Nullable_decimal_,System.Collections.Generic.IReadOnlyList_TabletopCardEngine.Card_,TabletopCardEngine.Holdem.Dto.PlayerActionData,TabletopCardEngine.Holdem.Dto.HandResultData).HandResult'></a>

`HandResult` [HandResultData](TabletopCardEngine.Holdem.Dto.HandResultData.md 'TabletopCardEngine\.Holdem\.Dto\.HandResultData')

Set once the hand is over; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') while it is still running\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.SeatData.BetSize'></a>

## SeatData\.BetSize Property

The seat's wager on the street now on the clock — its highest cumulative amount in the ledger for that street, the same street [StreetPot](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.StreetPot 'TabletopCardEngine\.Holdem\.Dto\.HandData\.StreetPot') counts; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when the seat has no action on that street yet \(a new street opens with everyone at [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\)\.

```csharp
public System.Nullable<decimal> BetSize { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.HandResult'></a>

## SeatData\.HandResult Property

Set once the hand is over; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') while it is still running\.

```csharp
public TabletopCardEngine.Holdem.Dto.HandResultData? HandResult { get; init; }
```

#### Property Value
[HandResultData](TabletopCardEngine.Holdem.Dto.HandResultData.md 'TabletopCardEngine\.Holdem\.Dto\.HandResultData')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.HasLeft'></a>

## SeatData\.HasLeft Property

Whether the seat's player has left with [HoldemActionLeaveSeat](TabletopCardEngine.Holdem.Dto.HoldemActionLeaveSeat.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionLeaveSeat') after the current hand\. The seat
still shows how that hand ended for the player; it is dealt out of the next hand and free for a new player\.

```csharp
public bool HasLeft { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.Hole'></a>

## SeatData\.Hole Property

The seat's hole cards, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when they are not visible to the caller\. Record equality compares it element by element for values the engine returns\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card>? Hole { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.IsActive'></a>

## SeatData\.IsActive Property

Whether the seat is still contesting the pot — it has chips and cards, or it is all in\.

```csharp
public bool IsActive { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.IsAllIn'></a>

## SeatData\.IsAllIn Property

Whether the seat has committed its whole stack\.

```csharp
public bool IsAllIn { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.IsFolded'></a>

## SeatData\.IsFolded Property

Whether the seat has folded out of the hand\.

```csharp
public bool IsFolded { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.LastAction'></a>

## SeatData\.LastAction Property

The most recent action this seat took, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') if it has not acted\.

```csharp
public TabletopCardEngine.Holdem.Dto.PlayerActionData? LastAction { get; init; }
```

#### Property Value
[PlayerActionData](TabletopCardEngine.Holdem.Dto.PlayerActionData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionData')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.Name'></a>

## SeatData\.Name Property

Display name of the player in the seat\.

```csharp
public string Name { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.SeatNo'></a>

## SeatData\.SeatNo Property

The seat number\. Seats are 1\-based; `0` means "no seat"\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.Stack'></a>

## SeatData\.Stack Property

The stack right now — StartHandStack minus everything wagered this hand\.

```csharp
public decimal Stack { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.SeatData.StartHandStack'></a>

## SeatData\.StartHandStack Property

The stack this seat sat down with at the start of the hand\.

```csharp
public decimal StartHandStack { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
