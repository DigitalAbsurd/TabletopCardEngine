#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemTopUpRequest Class

Carries the chips for a `POST /games/{gameId}/seats/{seatNo}/top-up` request\.

```csharp
public sealed record HoldemTopUpRequest : System.IEquatable<TabletopCardEngine.AspNetCore.HoldemTopUpRequest>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemTopUpRequest

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemTopUpRequest](TabletopCardEngine.AspNetCore.HoldemTopUpRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemTopUpRequest')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemTopUpRequest.Amount'></a>

## HoldemTopUpRequest\.Amount Property

The chips to add to the seat for the next hand, with at most two decimal places\.

```csharp
public decimal Amount { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')
