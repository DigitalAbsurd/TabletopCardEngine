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
