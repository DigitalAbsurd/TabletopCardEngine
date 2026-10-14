#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Dto](TabletopCardEngine.Holdem.Dto.md 'TabletopCardEngine\.Holdem\.Dto')

## HoldemAction Class

The one command type the engine accepts\. Which concrete record you pass decides which handler
runs — see `IHoldemActionHandler.CreateHandlerForAction`\. A new action type has to be added
both here and to that switch\. Every command is a `HoldemAction<Verb>` record with one
static factory of the same verb on this type\. The shape rule for the records: required data is
positional, optional data is an [init](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/init') property\.

```csharp
public abstract record HoldemAction : System.IEquatable<TabletopCardEngine.Holdem.Dto.HoldemAction>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemAction

Derived  
↳ [HoldemActionCreateGame](TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionCreateGame')  
↳ [HoldemActionFinishGame](TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionFinishGame')  
↳ [HoldemActionGetGame](TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionGetGame')  
↳ [HoldemActionPlay](TabletopCardEngine.Holdem.Dto.HoldemActionPlay.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionPlay')  
↳ [HoldemActionStartGame](TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartGame')  
↳ [HoldemActionStartHand](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand')

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[HoldemAction](TabletopCardEngine.Holdem.Dto.HoldemAction.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemAction')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.HoldemAction(System.Guid)'></a>

## HoldemAction\(Guid\) Constructor

The one command type the engine accepts\. Which concrete record you pass decides which handler
runs — see `IHoldemActionHandler.CreateHandlerForAction`\. A new action type has to be added
both here and to that switch\. Every command is a `HoldemAction<Verb>` record with one
static factory of the same verb on this type\. The shape rule for the records: required data is
positional, optional data is an [init](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/init') property\.

```csharp
protected HoldemAction(System.Guid GameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.HoldemAction(System.Guid).GameId'></a>

`GameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game the action applies to\.
### Properties

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.ExpectedVersion'></a>

## HoldemAction\.ExpectedVersion Property

The [Version](TabletopCardEngine.Holdem.Dto.GameData.md#TabletopCardEngine.Holdem.Dto.GameData.Version 'TabletopCardEngine\.Holdem\.Dto\.GameData\.Version') the caller last saw, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to skip the check\.
When set and different from the game's current version, the call fails with
[Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict') before anything runs\. Ignored by [HoldemActionCreateGame](TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionCreateGame'),
which has no game to compare against\.

```csharp
public System.Nullable<int> ExpectedVersion { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.GameId'></a>

## HoldemAction\.GameId Property

The game the action applies to\.

```csharp
public System.Guid GameId { get; init; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')
### Methods

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.CreateGame(TabletopCardEngine.Holdem.Dto.GameOptionData,System.Nullable_System.Guid_)'></a>

## HoldemAction\.CreateGame\(GameOptionData, Nullable&lt;Guid&gt;\) Method

Creates a game with the given rules, generating an identifier when none is supplied\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame CreateGame(TabletopCardEngine.Holdem.Dto.GameOptionData options, System.Nullable<System.Guid> gameId=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.CreateGame(TabletopCardEngine.Holdem.Dto.GameOptionData,System.Nullable_System.Guid_).options'></a>

`options` [GameOptionData](TabletopCardEngine.Holdem.Dto.GameOptionData.md 'TabletopCardEngine\.Holdem\.Dto\.GameOptionData')

The rules the game is created with\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.CreateGame(TabletopCardEngine.Holdem.Dto.GameOptionData,System.Nullable_System.Guid_).gameId'></a>

`gameId` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Identifier to use, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to generate one\.

#### Returns
[HoldemActionCreateGame](TabletopCardEngine.Holdem.Dto.HoldemActionCreateGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionCreateGame')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.FinishGame(System.Guid)'></a>

## HoldemAction\.FinishGame\(Guid\) Method

Closes the game, between hands only\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame FinishGame(System.Guid gameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.FinishGame(System.Guid).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to finish\.

#### Returns
[HoldemActionFinishGame](TabletopCardEngine.Holdem.Dto.HoldemActionFinishGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionFinishGame')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.GetGame(System.Guid)'></a>

## HoldemAction\.GetGame\(Guid\) Method

Reads the game back without changing anything\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionGetGame GetGame(System.Guid gameId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.GetGame(System.Guid).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to read\.

#### Returns
[HoldemActionGetGame](TabletopCardEngine.Holdem.Dto.HoldemActionGetGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionGetGame')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.Play(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal)'></a>

## HoldemAction\.Play\(Guid, PlayerActionType, decimal\) Method

Plays one action for the seat whose turn it is\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionPlay Play(System.Guid gameId, TabletopCardEngine.Holdem.PlayerActionType actionType, decimal size=0m);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.Play(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to act in\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.Play(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).actionType'></a>

`actionType` [PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType')

What the seat does\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.Play(System.Guid,TabletopCardEngine.Holdem.PlayerActionType,decimal).size'></a>

`size` [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal')

For a raise or a bet, the street total to raise or bet &lt;i&gt;to&lt;/i&gt;; ignored for actions that carry no size\.

#### Returns
[HoldemActionPlay](TabletopCardEngine.Holdem.Dto.HoldemActionPlay.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionPlay')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartGame(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[])'></a>

## HoldemAction\.StartGame\(Guid, PlayerJoinData\[\]\) Method

Starts a created game: seats the players and deals hand 1\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionStartGame StartGame(System.Guid gameId, TabletopCardEngine.Holdem.Dto.PlayerJoinData[] joinSeats);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartGame(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[]).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to start\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartGame(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[]).joinSeats'></a>

`joinSeats` [PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The seats to fill, in any order\.

#### Returns
[HoldemActionStartGame](TabletopCardEngine.Holdem.Dto.HoldemActionStartGame.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartGame')

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartHand(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[])'></a>

## HoldemAction\.StartHand\(Guid, PlayerJoinData\[\]\) Method

Deals the next hand of a started game, optionally seating players first\.

```csharp
public static TabletopCardEngine.Holdem.Dto.HoldemActionStartHand StartHand(System.Guid gameId, TabletopCardEngine.Holdem.Dto.PlayerJoinData[]? joinSeats=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartHand(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[]).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game to deal in\.

<a name='TabletopCardEngine.Holdem.Dto.HoldemAction.StartHand(System.Guid,TabletopCardEngine.Holdem.Dto.PlayerJoinData[]).joinSeats'></a>

`joinSeats` [PlayerJoinData](TabletopCardEngine.Holdem.Dto.PlayerJoinData.md 'TabletopCardEngine\.Holdem\.Dto\.PlayerJoinData')[\[\]](https://learn.microsoft.com/en-us/dotnet/api/system.array 'System\.Array')

The seats to fill before dealing, for players who sit down between hands, or
[null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to deal to the players already seated\.

#### Returns
[HoldemActionStartHand](TabletopCardEngine.Holdem.Dto.HoldemActionStartHand.md 'TabletopCardEngine\.Holdem\.Dto\.HoldemActionStartHand')
