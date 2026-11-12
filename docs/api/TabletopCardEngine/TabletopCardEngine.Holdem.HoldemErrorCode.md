#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemErrorCode Enum

Machine\-readable reason an action was rejected\. The numeric values are part of the contract:
they are explicit, never renumbered and never reused, so a caller may persist or switch on them\.
There is deliberately no zero member, so `default` is never a valid code\.

```csharp
public enum HoldemErrorCode
```
### Fields

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.GameAlreadyExists'></a>

`GameAlreadyExists` 1

A game with the requested id already exists\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.CurrentHandEnded'></a>

`CurrentHandEnded` 2

The current hand is completed, so no player action can be applied to it\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.PreviousHandNotEnded'></a>

`PreviousHandNotEnded` 3

The previous hand is not completed, so a new hand cannot start yet\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidAction'></a>

`InvalidAction` 4

The player action's type is not a defined [PlayerActionType](TabletopCardEngine.Holdem.PlayerActionType.md 'TabletopCardEngine\.Holdem\.PlayerActionType') member\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.UnsupportedGameOptions'></a>

`UnsupportedGameOptions` 5

The game's [GameType](TabletopCardEngine.Holdem.GameType.md 'TabletopCardEngine\.Holdem\.GameType') and [BettingStructure](TabletopCardEngine.Holdem.BettingStructure.md 'TabletopCardEngine\.Holdem\.BettingStructure') is not a supported combination\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidGameOptions'></a>

`InvalidGameOptions` 6

A game option is out of range: seat count, small blind or first dealer seat\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidSeat'></a>

`InvalidSeat` 7

A seat to join is out of range, repeated, already occupied, or has a non\-positive buy\-in\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.GameAlreadyStarted'></a>

`GameAlreadyStarted` 8

The game already has a hand dealt, or was already started, so it cannot be started again\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.GameNotStarted'></a>

`GameNotStarted` 9

No hand has been dealt yet, so the action needs `StartGame` first\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.GameFinished'></a>

`GameFinished` 10

The game was finished, so only reading it back is allowed\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.Conflict'></a>

`Conflict` 11

The game was changed by another action since the caller read it, so nothing was applied\. Reload it and decide\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.GameNotFound'></a>

`GameNotFound` 12

No game with the requested id exists\. Every action except `CreateGame` needs one\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.IllegalAction'></a>

`IllegalAction` 13

The action is not one the seat to act may take now, or its size is outside the window offered\.
[Evaluations](TabletopCardEngine.Holdem.Dto.HandData.md#TabletopCardEngine.Holdem.Dto.HandData.Evaluations 'TabletopCardEngine\.Holdem\.Dto\.HandData\.Evaluations') lists what is legal; nothing was applied\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.NotEnoughPlayers'></a>

`NotEnoughPlayers` 14

Fewer than two seats would have chips to play the hand: seat more players with `StartHand` and try again\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidBuyIn'></a>

`InvalidBuyIn` 15

A buy\-in, top\-up or rebuy amount is outside the game's buy\-in window \(`MinBuyIn`/`MaxBuyIn`\), or would take the stack above `MaxBuyIn`\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TopUpNotAllowed'></a>

`TopUpNotAllowed` 16

The seat still has chips and the game does not allow topping up \(`AllowTopUp` is off\), so nothing was added\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.RebuyNotAllowed'></a>

`RebuyNotAllowed` 17

The seat has no chips and the game does not allow a rebuy \(`AllowRebuy` is off\), or the player has used up `MaxRebuys`\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentNotFound'></a>

`TournamentNotFound` 18

No tournament with the requested id exists\. Every tournament action except `CreateTournament` needs one\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyExists'></a>

`TournamentAlreadyExists` 19

A tournament with the requested id already exists\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTournamentOptions'></a>

`InvalidTournamentOptions` 20

A tournament option is out of range: the table format and seats, an amount, the entry limits, the levels or the payouts\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentAlreadyStarted'></a>

`TournamentAlreadyStarted` 21

The tournament is no longer registering, so its entries cannot change\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFull'></a>

`TournamentFull` 22

The tournament already has `MaxEntries` entries, so nobody else can register\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidEntry'></a>

`InvalidEntry` 23

The entry is invalid: a name that is empty, too long or already registered, or an entry number that is not registered\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.ManagedByTournament'></a>

`ManagedByTournament` 24

The game is a table of a tournament, which deals its hands, seats and moves its players and ends it: only
`Play` and `GetGame` may be applied to it through `IHoldemTable`\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentNotStarted'></a>

`TournamentNotStarted` 25

The tournament is still registering, so it has no table to deal\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.TournamentFinished'></a>

`TournamentFinished` 26

The tournament has finished: its places and prizes are final, and its tables deal no more hands\.

<a name='TabletopCardEngine.Holdem.HoldemErrorCode.InvalidTable'></a>

`InvalidTable` 27

The tournament has no table with the requested number, or that table has been broken up\.
