#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## PlayerJoinData Class

A player taking a seat, together with the stack they bring to it\.

```csharp
public record PlayerJoinData : System.IEquatable<TabletopCardEngine.Holdem.Dto.PlayerJoinData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → PlayerJoinData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.PlayerJoinData(string,byte,decimal)'></a>

## PlayerJoinData\(string, byte, decimal\) Constructor

A player taking a seat, together with the stack they bring to it\.

```csharp
public PlayerJoinData(string Name, byte SeatNo, decimal BuyInAmount);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.PlayerJoinData(string,byte,decimal).Name'></a>

`Name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Display name of the player; must be 1 to 64 characters and not null or whitespace\-only\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.PlayerJoinData(string,byte,decimal).SeatNo'></a>

`SeatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat to occupy\. Seats are 1\-based; `0` means "no seat"\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.PlayerJoinData(string,byte,decimal).BuyInAmount'></a>

`BuyInAmount` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

The stack the player sits down with; must have at most two decimal places\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.BuyInAmount'></a>

## PlayerJoinData\.BuyInAmount Property

The stack the player sits down with; must have at most two decimal places\.

```csharp
public decimal BuyInAmount { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.Name'></a>

## PlayerJoinData\.Name Property

Display name of the player; must be 1 to 64 characters and not null or whitespace\-only\.

```csharp
public string Name { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.Dto.PlayerJoinData.SeatNo'></a>

## PlayerJoinData\.SeatNo Property

The seat to occupy\. Seats are 1\-based; `0` means "no seat"\.

```csharp
public byte SeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')
