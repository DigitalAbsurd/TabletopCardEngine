#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemResult&lt;T&gt; Class

The outcome of one engine call: a value on success, or the errors that rejected the action\.

```csharp
public sealed class HoldemResult<T>
```
#### Type parameters

<a name='TabletopCardEngine.Holdem.HoldemResult_T_.T'></a>

`T`

The type of the value carried on success\.

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemResult&lt;T&gt;
### Properties

<a name='TabletopCardEngine.Holdem.HoldemResult_T_.Errors'></a>

## HoldemResult&lt;T&gt;\.Errors Property

The reasons the action was rejected; empty on success\.

```csharp
public System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Holdem.HoldemError> Errors { get; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[HoldemError](TabletopCardEngine.Holdem.HoldemError.md 'TabletopCardEngine\.Holdem\.HoldemError')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='TabletopCardEngine.Holdem.HoldemResult_T_.IsFailed'></a>

## HoldemResult&lt;T&gt;\.IsFailed Property

Whether the action was rejected\.

```csharp
public bool IsFailed { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.HoldemResult_T_.IsSuccess'></a>

## HoldemResult&lt;T&gt;\.IsSuccess Property

Whether the action was applied\.

```csharp
public bool IsSuccess { get; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.HoldemResult_T_.Value'></a>

## HoldemResult&lt;T&gt;\.Value Property

The value of a successful call\.

```csharp
public T Value { get; }
```

#### Property Value
[T](TabletopCardEngine.Holdem.HoldemResult_T_.md#TabletopCardEngine.Holdem.HoldemResult_T_.T 'TabletopCardEngine\.Holdem\.HoldemResult&lt;T&gt;\.T')

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
The result is failed, so there is no value\.
