#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## ICardShuffler Interface

Supplies pre\-determined cards instead of a real shuffle, which is what makes replaying a
recorded hand history possible\. Register one with `HoldemOptionsBuilder.WithCardShuffler`;
leave it unregistered and every deck is drawn at random, excluding the cards already in play\.

Both methods are keyed by hand number, so a single implementation can serve a whole multi-hand
session.

```csharp
public interface ICardShuffler
```
### Methods

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetDeckCardsAsync(System.Guid,int,System.Threading.CancellationToken)'></a>

## ICardShuffler\.GetDeckCardsAsync\(Guid, int, CancellationToken\) Method

The board cards for the hand, in dealt order\. Called again before each street change, so it
must return the same sequence every time it is asked about the same hand\.

```csharp
System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card>> GetDeckCardsAsync(System.Guid gameId, int handNo, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetDeckCardsAsync(System.Guid,int,System.Threading.CancellationToken).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game being dealt\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetDeckCardsAsync(System.Guid,int,System.Threading.CancellationToken).handNo'></a>

`handNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

Hand number within the game, ascending from 1\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetDeckCardsAsync(System.Guid,int,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The token the caller passed to `ApplyAsync`\. It has no default, so an
implementation that does I/O sees it and can honour it\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
The cards, read once per call and not retained by the engine\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetHoleCardsAsync(System.Guid,int,byte,System.Threading.CancellationToken)'></a>

## ICardShuffler\.GetHoleCardsAsync\(Guid, int, byte, CancellationToken\) Method

The hole cards to deal to one seat, in dealt order\.

```csharp
System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyList<TabletopCardEngine.Card>> GetHoleCardsAsync(System.Guid gameId, int handNo, byte seatNo, System.Threading.CancellationToken cancellationToken);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetHoleCardsAsync(System.Guid,int,byte,System.Threading.CancellationToken).gameId'></a>

`gameId` [System\.Guid](https://learn.microsoft.com/en-us/dotnet/api/system.guid 'System\.Guid')

The game being dealt\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetHoleCardsAsync(System.Guid,int,byte,System.Threading.CancellationToken).handNo'></a>

`handNo` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

Hand number within the game, ascending from 1\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetHoleCardsAsync(System.Guid,int,byte,System.Threading.CancellationToken).seatNo'></a>

`seatNo` [System\.Byte](https://learn.microsoft.com/en-us/dotnet/api/system.byte 'System\.Byte')

The seat to deal to\. Seats are 1\-based\.

<a name='TabletopCardEngine.Holdem.ICardShuffler.GetHoleCardsAsync(System.Guid,int,byte,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

The token the caller passed to `ApplyAsync`\. It has no default, so an
implementation that does I/O sees it and can honour it\.

#### Returns
[System\.Threading\.Tasks\.ValueTask&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1 'System\.Threading\.Tasks\.ValueTask\`1')  
The cards, read once per call and not retained by the engine\.
