#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemError Class

One reason an action was rejected\.

```csharp
public sealed record HoldemError : System.IEquatable<TabletopCardEngine.Holdem.HoldemError>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemError

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemError](TabletopCardEngine.Holdem.HoldemError.md 'TabletopCardEngine\.Holdem\.HoldemError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.HoldemError.HoldemError(TabletopCardEngine.Holdem.HoldemErrorCode,string)'></a>

## HoldemError\(HoldemErrorCode, string\) Constructor

One reason an action was rejected\.

```csharp
public HoldemError(TabletopCardEngine.Holdem.HoldemErrorCode Code, string Message);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.HoldemError.HoldemError(TabletopCardEngine.Holdem.HoldemErrorCode,string).Code'></a>

`Code` [HoldemErrorCode](TabletopCardEngine.Holdem.HoldemErrorCode.md 'TabletopCardEngine\.Holdem\.HoldemErrorCode')

The stable, machine\-readable reason\.

<a name='TabletopCardEngine.Holdem.HoldemError.HoldemError(TabletopCardEngine.Holdem.HoldemErrorCode,string).Message'></a>

`Message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The localised, human\-readable text for [Code](TabletopCardEngine.Holdem.HoldemError.md#TabletopCardEngine.Holdem.HoldemError.HoldemError(TabletopCardEngine.Holdem.HoldemErrorCode,string).Code 'TabletopCardEngine\.Holdem\.HoldemError\.HoldemError\(TabletopCardEngine\.Holdem\.HoldemErrorCode, string\)\.Code')\.
### Properties

<a name='TabletopCardEngine.Holdem.HoldemError.Code'></a>

## HoldemError\.Code Property

The stable, machine\-readable reason\.

```csharp
public TabletopCardEngine.Holdem.HoldemErrorCode Code { get; init; }
```

#### Property Value
[HoldemErrorCode](TabletopCardEngine.Holdem.HoldemErrorCode.md 'TabletopCardEngine\.Holdem\.HoldemErrorCode')

<a name='TabletopCardEngine.Holdem.HoldemError.Message'></a>

## HoldemError\.Message Property

The localised, human\-readable text for Code\.

```csharp
public string Message { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
