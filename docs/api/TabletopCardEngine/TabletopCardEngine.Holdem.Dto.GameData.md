#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## GameData Class

Everything the engine knows about a game, as returned by every call\.

```csharp
public record GameData : System.IEquatable<TabletopCardEngine.Holdem.Dto.GameData>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → GameData

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[GameData](TabletopCardEngine.Holdem.Dto.GameData.md 'TabletopCardEngine\.Holdem\.Dto\.GameData')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int)'></a>

## GameData\(Guid, GameOptionData, HandData, bool, int\) Constructor

Everything the engine knows about a game, as returned by every call\.

```csharp
public GameData(System.Guid GameId, TabletopCardEngine.Holdem.Dto.GameOptionData Options, TabletopCardEngine.Holdem.Dto.HandData? HandData, bool IsFinished=false, int Version=0);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

Identifier of the game\.

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int).Options'></a>

`Options` [GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')

The rules the game was created with\.

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int).HandData'></a>

`HandData` [HandData](TabletopCardEngine.Holdem.Dto.HandData.md 'TabletopCardEngine\.Holdem\.Dto\.HandData')

The current hand, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when no hand has been dealt yet\.

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int).IsFinished'></a>

`IsFinished` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether `FinishGame` has closed the game; after that only reading it back is allowed\.

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameData(System.Guid,TabletopCardEngine.Holdem.Dto.GameOptionData,TabletopCardEngine.Holdem.Dto.HandData,bool,int).Version'></a>

`Version` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

How many times the game has been saved, starting at `1` on creation\. Pass it back as
[ExpectedVersion](TabletopCardEngine.Holdem.Dto.HoldemAction.md#TabletopCardEngine.Holdem.Dto.HoldemAction.ExpectedVersion 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction\.ExpectedVersion') to have a stale write rejected with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict')\.
Reading the game does not change it\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.GameData.GameId'></a>

## GameData\.GameId Property

Identifier of the game\.

```csharp
public System.Guid GameId { get; init; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

<a name='TabletopCardEngine.Holdem.Dto.GameData.HandData'></a>

## GameData\.HandData Property

The current hand, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when no hand has been dealt yet\.

```csharp
public TabletopCardEngine.Holdem.Dto.HandData? HandData { get; init; }
```

#### Property Value
[HandData](TabletopCardEngine.Holdem.Dto.HandData.md 'TabletopCardEngine\.Holdem\.Dto\.HandData')

<a name='TabletopCardEngine.Holdem.Dto.GameData.IsFinished'></a>

## GameData\.IsFinished Property

Whether `FinishGame` has closed the game; after that only reading it back is allowed\.

```csharp
public bool IsFinished { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Dto.GameData.Options'></a>

## GameData\.Options Property

The rules the game was created with\.

```csharp
public TabletopCardEngine.Holdem.Dto.GameOptionData Options { get; init; }
```

#### Property Value
[GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')

<a name='TabletopCardEngine.Holdem.Dto.GameData.Version'></a>

## GameData\.Version Property

How many times the game has been saved, starting at `1` on creation\. Pass it back as
[ExpectedVersion](TabletopCardEngine.Holdem.Dto.HoldemAction.md#TabletopCardEngine.Holdem.Dto.HoldemAction.ExpectedVersion 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction\.ExpectedVersion') to have a stale write rejected with [Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict')\.
Reading the game does not change it\.

```csharp
public int Version { get; init; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
