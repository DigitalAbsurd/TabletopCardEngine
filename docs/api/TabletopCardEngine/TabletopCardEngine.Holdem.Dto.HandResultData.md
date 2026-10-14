#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HandResultData Class

What a seat walked away from a finished hand with\.

```csharp
public record HandResultData : System.IEquatable<TabletopCardEngine.Holdem.Dto.HandResultData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HandResultData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HandResultData](TabletopCardEngine.Holdem.Dto.HandResultData.md 'TabletopCardEngine\.Holdem\.Dto\.HandResultData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.HandResultData(decimal,uint,TabletopCardEngine.Holdem.HandRankType,string)'></a>

## HandResultData\(decimal, uint, HandRankType, string\) Constructor

What a seat walked away from a finished hand with\.

```csharp
public HandResultData(decimal Net, uint Strength, TabletopCardEngine.Holdem.HandRankType RankType, string? Description=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.HandResultData(decimal,uint,TabletopCardEngine.Holdem.HandRankType,string).Net'></a>

`Net` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

Chips won minus chips wagered over the whole hand; negative for a loser\.

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.HandResultData(decimal,uint,TabletopCardEngine.Holdem.HandRankType,string).Strength'></a>

`Strength` [System\.UInt32](https://learn.microsoft.com/en-us/dotnet/api/system.uint32 'System\.UInt32')

A comparable hand\-strength value — only meaningful against another seat's value in the same hand\.

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.HandResultData(decimal,uint,TabletopCardEngine.Holdem.HandRankType,string).RankType'></a>

`RankType` [HandRankType](TabletopCardEngine.Holdem.HandRankType.md 'TabletopCardEngine\.Holdem\.HandRankType')

The display category of the made hand\.

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.HandResultData(decimal,uint,TabletopCardEngine.Holdem.HandRankType,string).Description'></a>

`Description` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

Human\-readable name of the made hand's category, such as "Two pair"; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when the seat did not reach showdown \(it folded, or the hand ended because everyone else folded\)\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.Description'></a>

## HandResultData\.Description Property

Human\-readable name of the made hand's category, such as "Two pair"; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when the seat did not reach showdown \(it folded, or the hand ended because everyone else folded\)\.

```csharp
public string? Description { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.Net'></a>

## HandResultData\.Net Property

Chips won minus chips wagered over the whole hand; negative for a loser\.

```csharp
public decimal Net { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.RankType'></a>

## HandResultData\.RankType Property

The display category of the made hand\.

```csharp
public TabletopCardEngine.Holdem.HandRankType RankType { get; init; }
```

#### Property Value
[HandRankType](TabletopCardEngine.Holdem.HandRankType.md 'TabletopCardEngine\.Holdem\.HandRankType')

<a name='TabletopCardEngine.Holdem.Dto.HandResultData.Strength'></a>

## HandResultData\.Strength Property

A comparable hand\-strength value — only meaningful against another seat's value in the same hand\.

```csharp
public uint Strength { get; init; }
```

#### Property Value
[System\.UInt32](https://learn.microsoft.com/en-us/dotnet/api/system.uint32 'System\.UInt32')
