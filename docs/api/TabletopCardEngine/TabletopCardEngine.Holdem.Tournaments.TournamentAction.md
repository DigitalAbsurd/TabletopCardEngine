#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Tournaments](TabletopCardEngine.Holdem.Tournaments.md 'TabletopCardEngine\.Holdem\.Tournaments')

## TournamentAction Class

The one command type [IHoldemTournament](TabletopCardEngine.Holdem.Tournaments.IHoldemTournament.md 'TabletopCardEngine\.Holdem\.Tournaments\.IHoldemTournament') accepts\. Which concrete record you pass decides what happens\.
Every command is a `TournamentAction<Verb>` record with one static factory on this type\. The shape
rule for the records: required data is positional, optional data is an [init](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/init') property\.

```csharp
public abstract record TournamentAction : System.IEquatable<TabletopCardEngine.Holdem.Tournaments.TournamentAction>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TournamentAction

Derived  
↳ [TournamentActionCreate](TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionCreate')  
↳ [TournamentActionGet](TabletopCardEngine.Holdem.Tournaments.TournamentActionGet.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionGet')  
↳ [TournamentActionRegister](TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionRegister')  
↳ [TournamentActionStart](TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionStart')  
↳ [TournamentActionUnregister](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister')

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[TournamentAction](TabletopCardEngine.Holdem.Tournaments.TournamentAction.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentAction')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.TournamentAction(System.Guid)'></a>

## TournamentAction\(Guid\) Constructor

The one command type [IHoldemTournament](TabletopCardEngine.Holdem.Tournaments.IHoldemTournament.md 'TabletopCardEngine\.Holdem\.Tournaments\.IHoldemTournament') accepts\. Which concrete record you pass decides what happens\.
Every command is a `TournamentAction<Verb>` record with one static factory on this type\. The shape
rule for the records: required data is positional, optional data is an [init](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/init') property\.

```csharp
protected TournamentAction(System.Guid TournamentId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.TournamentAction(System.Guid).TournamentId'></a>

`TournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament the action applies to\.
### Properties

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.ExpectedVersion'></a>

## TournamentAction\.ExpectedVersion Property

The [Version](TabletopCardEngine.Holdem.Tournaments.TournamentData.md#TabletopCardEngine.Holdem.Tournaments.TournamentData.Version 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentData\.Version') the caller last saw, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to skip the check\.
When set and different from the tournament's current version, the call fails with
[Conflict](TabletopCardEngine.Holdem.HoldemErrorCode.md#TabletopCardEngine.Holdem.HoldemErrorCode.Conflict 'TabletopCardEngine\.Holdem\.HoldemErrorCode\.Conflict') before anything runs\. Ignored by [TournamentActionCreate](TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionCreate'),
which has no tournament to compare against\.

```csharp
public System.Nullable<int> ExpectedVersion { get; init; }
```

#### Property Value
[System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.TournamentId'></a>

## TournamentAction\.TournamentId Property

The tournament the action applies to\.

```csharp
public System.Guid TournamentId { get; init; }
```

#### Property Value
[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')
### Methods

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.CreateTournament(TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,System.Nullable_System.Guid_)'></a>

## TournamentAction\.CreateTournament\(TournamentOptionData, Nullable&lt;Guid&gt;\) Method

Creates a tournament with the given rules, generating an identifier when none is supplied\.

```csharp
public static TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate CreateTournament(TabletopCardEngine.Holdem.Tournaments.TournamentOptionData options, System.Nullable<System.Guid> tournamentId=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.CreateTournament(TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,System.Nullable_System.Guid_).options'></a>

`options` [TournamentOptionData](TabletopCardEngine.Holdem.Tournaments.TournamentOptionData.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentOptionData')

The rules the tournament is created with\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.CreateTournament(TabletopCardEngine.Holdem.Tournaments.TournamentOptionData,System.Nullable_System.Guid_).tournamentId'></a>

`tournamentId` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

Identifier to use, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to generate one\.

#### Returns
[TournamentActionCreate](TabletopCardEngine.Holdem.Tournaments.TournamentActionCreate.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionCreate')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.GetTournament(System.Guid)'></a>

## TournamentAction\.GetTournament\(Guid\) Method

Reads the tournament back without changing anything\.

```csharp
public static TabletopCardEngine.Holdem.Tournaments.TournamentActionGet GetTournament(System.Guid tournamentId);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.GetTournament(System.Guid).tournamentId'></a>

`tournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to read\.

#### Returns
[TournamentActionGet](TabletopCardEngine.Holdem.Tournaments.TournamentActionGet.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionGet')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Register(System.Guid,string)'></a>

## TournamentAction\.Register\(Guid, string\) Method

Registers a player for a tournament that has not started yet\.

```csharp
public static TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister Register(System.Guid tournamentId, string name);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Register(System.Guid,string).tournamentId'></a>

`tournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to register for\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Register(System.Guid,string).name'></a>

`name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The player's display name, 1 to 64 characters, unique within the tournament regardless of case\.

#### Returns
[TournamentActionRegister](TabletopCardEngine.Holdem.Tournaments.TournamentActionRegister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionRegister')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.StartTournament(System.Guid,System.Nullable_int_)'></a>

## TournamentAction\.StartTournament\(Guid, Nullable&lt;int&gt;\) Method

Closes registration, draws the seats, opens the tables and deals their first hands\.

```csharp
public static TabletopCardEngine.Holdem.Tournaments.TournamentActionStart StartTournament(System.Guid tournamentId, System.Nullable<int> seatDrawSeed=null);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.StartTournament(System.Guid,System.Nullable_int_).tournamentId'></a>

`tournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to start\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.StartTournament(System.Guid,System.Nullable_int_).seatDrawSeed'></a>

`seatDrawSeed` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The seed of the seat draw, to reproduce one; [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') to draw one at random\.

#### Returns
[TournamentActionStart](TabletopCardEngine.Holdem.Tournaments.TournamentActionStart.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionStart')

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Unregister(System.Guid,int)'></a>

## TournamentAction\.Unregister\(Guid, int\) Method

Withdraws a registration from a tournament that has not started yet\.

```csharp
public static TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister Unregister(System.Guid tournamentId, int entryNo);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Unregister(System.Guid,int).tournamentId'></a>

`tournamentId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The tournament to withdraw from\.

<a name='TabletopCardEngine.Holdem.Tournaments.TournamentAction.Unregister(System.Guid,int).entryNo'></a>

`entryNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The [EntryNo](TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.md#TabletopCardEngine.Holdem.Tournaments.TournamentEntryData.EntryNo 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentEntryData\.EntryNo') the registration was given\.

#### Returns
[TournamentActionUnregister](TabletopCardEngine.Holdem.Tournaments.TournamentActionUnregister.md 'TabletopCardEngine\.Holdem\.Tournaments\.TournamentActionUnregister')
