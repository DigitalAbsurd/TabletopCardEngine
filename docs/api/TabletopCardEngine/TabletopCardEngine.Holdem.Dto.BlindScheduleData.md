#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## BlindScheduleData Class

The rules by which a game's blinds rise, chosen when the game is created\. A game without a schedule
keeps [InitialSmallBlind](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.InitialSmallBlind 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.InitialSmallBlind') for every hand\. The big blind is always twice the small blind\.

The engine has no clock, so every trigger is a count of hands or players and a game stays deterministic.
A schedule needs at least one trigger: [IncreaseEveryHands](TabletopCardEngine.Holdem.Dto.BlindScheduleData.md#TabletopCardEngine.Holdem.Dto.BlindScheduleData.IncreaseEveryHands 'TabletopCardEngine\.Holdem\.Dto\.BlindScheduleData\.IncreaseEveryHands'), a non-empty
[PlayerThresholds](TabletopCardEngine.Holdem.Dto.BlindScheduleData.md#TabletopCardEngine.Holdem.Dto.BlindScheduleData.PlayerThresholds 'TabletopCardEngine\.Holdem\.Dto\.BlindScheduleData\.PlayerThresholds'), or [IncreaseOnElimination](TabletopCardEngine.Holdem.Dto.BlindScheduleData.md#TabletopCardEngine.Holdem.Dto.BlindScheduleData.IncreaseOnElimination 'TabletopCardEngine\.Holdem\.Dto\.BlindScheduleData\.IncreaseOnElimination').

```csharp
public sealed record BlindScheduleData : System.IEquatable<TabletopCardEngine.Holdem.Dto.BlindScheduleData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → BlindScheduleData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[BlindScheduleData](TabletopCardEngine.Holdem.Dto.BlindScheduleData.md 'TabletopCardEngine\.Holdem\.Dto\.BlindScheduleData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.IncreaseEveryHands'></a>

## BlindScheduleData\.IncreaseEveryHands Property

Raise one level after this many hands at the current level; the count restarts after any increase\.
[null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') \(the default\) switches this trigger off; otherwise it must be at least 1\.

```csharp
public System.Nullable<int> IncreaseEveryHands { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.IncreaseOnElimination'></a>

## BlindScheduleData\.IncreaseOnElimination Property

Raise one level whenever fewer players are dealt into a hand than into the previous one: a bust, a
leave or a kick\. [false](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool') by default\.

```csharp
public bool IncreaseOnElimination { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.MaxSmallBlind'></a>

## BlindScheduleData\.MaxSmallBlind Property

[Multiplier](TabletopCardEngine.Holdem.BlindIncreaseMode.md#TabletopCardEngine.Holdem.BlindIncreaseMode.Multiplier 'TabletopCardEngine\.Holdem\.BlindIncreaseMode\.Multiplier') mode only: the small blind never goes above this\. When set
            it must be at least [InitialSmallBlind](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.InitialSmallBlind 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.InitialSmallBlind') and have at most two decimal
            places\. [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') \(the default\) means no cap\.

```csharp
public System.Nullable<decimal> MaxSmallBlind { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.Mode'></a>

## BlindScheduleData\.Mode Property

How the next level's small blind is worked out\. Defaults to [Multiplier](TabletopCardEngine.Holdem.BlindIncreaseMode.md#TabletopCardEngine.Holdem.BlindIncreaseMode.Multiplier 'TabletopCardEngine\.Holdem\.BlindIncreaseMode\.Multiplier'); an undefined value is rejected when the game is created\.

```csharp
public TabletopCardEngine.Holdem.BlindIncreaseMode Mode { get; init; }
```

#### Property Value
[BlindIncreaseMode](TabletopCardEngine.Holdem.BlindIncreaseMode.md 'TabletopCardEngine\.Holdem\.BlindIncreaseMode')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.Multiplier'></a>

## BlindScheduleData\.Multiplier Property

[Multiplier](TabletopCardEngine.Holdem.BlindIncreaseMode.md#TabletopCardEngine.Holdem.BlindIncreaseMode.Multiplier 'TabletopCardEngine\.Holdem\.BlindIncreaseMode\.Multiplier') mode: each level's small blind is the previous one times
            this, rounded to the cent\. Must be above 1 and have at most two decimal places\. Defaults to 2\.

```csharp
public decimal Multiplier { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.PlayerThresholds'></a>

## BlindScheduleData\.PlayerThresholds Property

Raise one level the first time the players dealt into a hand fall to or below each value; each value
fires once\. Values must be distinct and lie in `2..MaxPlayersCount - 1`\. Empty by default\.

```csharp
public System.Collections.Generic.IReadOnlyList<int> PlayerThresholds { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.Dto.BlindScheduleData.SmallBlindLevels'></a>

## BlindScheduleData\.SmallBlindLevels Property

[Levels](TabletopCardEngine.Holdem.BlindIncreaseMode.md#TabletopCardEngine.Holdem.BlindIncreaseMode.Levels 'TabletopCardEngine\.Holdem\.BlindIncreaseMode\.Levels') mode only: the small blinds after
            [InitialSmallBlind](TabletopCardEngine.Holdem.Dto.GameOptionData.md#TabletopCardEngine.Holdem.Dto.GameOptionData.InitialSmallBlind 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData\.InitialSmallBlind'), strictly increasing, the first above the initial one,
            each with at most two decimal places\. Once exhausted the last one holds\. Empty by default\.

```csharp
public System.Collections.Generic.IReadOnlyList<decimal> SmallBlindLevels { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')
