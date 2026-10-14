#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## GameOptionData Class

The fixed rules a game is created with; they never change once the game exists\.

This is where other games and betting structures will plug in: a new option is a new
property with a default, so existing callers keep compiling.

```csharp
public sealed record GameOptionData : System.IEquatable<TabletopCardEngine.Holdem.Dto.GameOptionData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → GameOptionData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.Holdem.Dto.GameOptionData.BettingStructure'></a>

## GameOptionData\.BettingStructure Property

How bets are sized\. Only [NoLimit](TabletopCardEngine.Holdem.BettingStructure.md#TabletopCardEngine.Holdem.BettingStructure.NoLimit 'TabletopCardEngine\.Holdem\.BettingStructure\.NoLimit') exists in v1\.

```csharp
public TabletopCardEngine.Holdem.BettingStructure BettingStructure { get; init; }
```

#### Property Value
[BettingStructure](TabletopCardEngine.Holdem.BettingStructure.md 'TabletopCardEngine\.Holdem\.BettingStructure')

<a name='TabletopCardEngine.Holdem.Dto.GameOptionData.GameType'></a>

## GameOptionData\.GameType Property

The poker variant\. Only [Holdem](TabletopCardEngine.Holdem.GameType.md#TabletopCardEngine.Holdem.GameType.Holdem 'TabletopCardEngine\.Holdem\.GameType\.Holdem') exists in v1\.

```csharp
public TabletopCardEngine.Holdem.GameType GameType { get; init; }
```

#### Property Value
[GameType](TabletopCardEngine.Holdem.GameType.md 'TabletopCardEngine\.Holdem\.GameType')

<a name='TabletopCardEngine.Holdem.Dto.GameOptionData.InitialDealerSeatNo'></a>

## GameOptionData\.InitialDealerSeatNo Property

Where the button sits for the first hand\. Seats are 1\-based\.

```csharp
public byte InitialDealerSeatNo { get; init; }
```

#### Property Value
[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

<a name='TabletopCardEngine.Holdem.Dto.GameOptionData.InitialSmallBlind'></a>

## GameOptionData\.InitialSmallBlind Property

The small blind of every hand in this release; the big blind is twice this\. Read a hand's actual blinds from [SmallBlind](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.SmallBlind 'TabletopCardEngine\.Holdem\.Dto\.HandData\.SmallBlind') and [BigBlind](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.BigBlind 'TabletopCardEngine\.Holdem\.Dto\.HandData\.BigBlind')\. Must be positive and have at most two decimal places\.

```csharp
public decimal InitialSmallBlind { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.GameOptionData.MaxPlayersCount'></a>

## GameOptionData\.MaxPlayersCount Property

How many seats the table has; must lie within the engine's configured seat limits, which are 2 to 6 in this release\.

```csharp
public int MaxPlayersCount { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
