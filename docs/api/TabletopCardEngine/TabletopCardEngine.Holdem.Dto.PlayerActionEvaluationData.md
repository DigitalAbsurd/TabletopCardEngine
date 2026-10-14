#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## PlayerActionEvaluationData Class

One action the seat to act may legally take, with its sizing window\.

The two bounds are on different bases by design: for [Call](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Call 'TabletopCardEngine\.Holdem\.PlayerActionType\.Call') they are
an increment ("how much more to put in"), while for [Raise](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Raise 'TabletopCardEngine\.Holdem\.PlayerActionType\.Raise') they are
both absolute street totals ("raise the street wager up to this") — the same basis
[Size](TabletopCardEngine.Holdem.Dto.HoldemActionPlay.md#TabletopCardEngine.Holdem.Dto.HoldemActionPlay.Size 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionPlay\.Size') expects. A "call" button wants a top-up amount; a
"raise to" slider wants an absolute.

```csharp
public record PlayerActionEvaluationData : System.IEquatable<TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → PlayerActionEvaluationData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[PlayerActionEvaluationData](TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerActionEvaluationData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.PlayerActionEvaluationData(TabletopCardEngine.Holdem.PlayerActionType,decimal,decimal)'></a>

## PlayerActionEvaluationData\(PlayerActionType, decimal, decimal\) Constructor

One action the seat to act may legally take, with its sizing window\.

The two bounds are on different bases by design: for [Call](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Call 'TabletopCardEngine\.Holdem\.PlayerActionType\.Call') they are
an increment ("how much more to put in"), while for [Raise](TabletopCardEngine.Holdem.PlayerActionType.md#TabletopCardEngine.Holdem.PlayerActionType.Raise 'TabletopCardEngine\.Holdem\.PlayerActionType\.Raise') they are
both absolute street totals ("raise the street wager up to this") — the same basis
[Size](TabletopCardEngine.Holdem.Dto.HoldemActionPlay.md#TabletopCardEngine.Holdem.Dto.HoldemActionPlay.Size 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionPlay\.Size') expects. A "call" button wants a top-up amount; a
"raise to" slider wants an absolute.

```csharp
public PlayerActionEvaluationData(TabletopCardEngine.Holdem.PlayerActionType PlayerActionType, decimal MinValue=0m, decimal MaxValue=0m);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.PlayerActionEvaluationData(TabletopCardEngine.Holdem.PlayerActionType,decimal,decimal).PlayerActionType'></a>

`PlayerActionType` [PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

The action this window describes\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.PlayerActionEvaluationData(TabletopCardEngine.Holdem.PlayerActionType,decimal,decimal).MinValue'></a>

`MinValue` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

Smallest legal amount, on the basis described above\.

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.PlayerActionEvaluationData(TabletopCardEngine.Holdem.PlayerActionType,decimal,decimal).MaxValue'></a>

`MaxValue` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

Largest legal amount, on the basis described above\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.MaxValue'></a>

## PlayerActionEvaluationData\.MaxValue Property

Largest legal amount, on the basis described above\.

```csharp
public decimal MaxValue { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.MinValue'></a>

## PlayerActionEvaluationData\.MinValue Property

Smallest legal amount, on the basis described above\.

```csharp
public decimal MinValue { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.PlayerActionEvaluationData.PlayerActionType'></a>

## PlayerActionEvaluationData\.PlayerActionType Property

The action this window describes\.

```csharp
public TabletopCardEngine.Holdem.PlayerActionType PlayerActionType { get; init; }
```

#### Property Value
[PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')
