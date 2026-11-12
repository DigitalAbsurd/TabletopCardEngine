#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentEntryData Class

One registered player of a tournament\.

```csharp
public record TournamentEntryData : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentEntryData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentEntryData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentEntryData](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TournamentEntryData(int,string)'></a>

## TournamentEntryData\(int, string\) Constructor

One registered player of a tournament\.

```csharp
public TournamentEntryData(int EntryNo, string Name);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TournamentEntryData(int,string).EntryNo'></a>

`EntryNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The entry's number, given at registration: one more than the highest given before, never reused\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TournamentEntryData(int,string).Name'></a>

`Name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The player's display name, unique within the tournament regardless of case\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo'></a>

## TournamentEntryData\.EntryNo Property

The entry's number, given at registration: one more than the highest given before, never reused\.

```csharp
public int EntryNo { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.Name'></a>

## TournamentEntryData\.Name Property

The player's display name, unique within the tournament regardless of case\.

```csharp
public string Name { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.Place'></a>

## TournamentEntryData\.Place Property

The player's finishing place, `1` for the winner; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') while still playing\.

```csharp
public System.Nullable<int> Place { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.Prize'></a>

## TournamentEntryData\.Prize Property

What the player won; `0` until a paid place is reached\.

```csharp
public decimal Prize { get; init; }
```

#### Property Value
[System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.SeatNo'></a>

## TournamentEntryData\.SeatNo Property

The seat the player sits in at [TableNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TableNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.TableNo'); [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when not seated\. Seats are 1\-based\.

```csharp
public System.Nullable<byte> SeatNo { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.State'></a>

## TournamentEntryData\.State Property

Where the player is in the tournament\.

```csharp
public TabletopCardEngine.Holdem.Tournaments.TournamentEntryState State { get; init; }
```

#### Property Value
[TournamentEntryState](TabletopCardEngine.Holdem.Tournaments.TournamentEntryState.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryState')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.TableNo'></a>

## TournamentEntryData\.TableNo Property

The table the player sits at; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when not seated\.

```csharp
public System.Nullable<int> TableNo { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')
