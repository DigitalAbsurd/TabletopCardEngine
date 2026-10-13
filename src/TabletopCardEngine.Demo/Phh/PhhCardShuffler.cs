using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Phh;

/// <summary>
/// Supplies pre-determined cards from a PHH hand history, implementing <see cref="ICardShuffler"/>
/// so the engine deals the exact cards recorded in the hand.
///
/// Stores a deck per hand so multiple hands can be replayed in sequence, each with its own cards.
/// <see cref="GetDeckCardsAsync"/> is called again before every street change and must return
/// the same board every time for the same hand.
///
/// This is modelled on <c>TabletopCardEngine.Tests.TestCardShuffler</c> but keyed by PHH hand
/// rather than by lambda callbacks.
/// </summary>
public sealed class PhhCardShuffler : ICardShuffler
{
    private readonly Dictionary<(Guid gameId, int handNo), PhhHandDeck> _decks = new();

    /// <summary>
    /// Registers the deck for a hand about to be replayed.
    /// </summary>
    public void RegisterHand(Guid gameId, int handNo, PhhHandDeck deck)
    {
        _decks[(gameId, handNo)] = deck ?? throw new ArgumentNullException(nameof(deck));
    }

    /// <summary>
    /// Clears all registered decks (call when ending a replay session).
    /// </summary>
    public void Clear()
    {
        _decks.Clear();
    }

    public ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)
    {
        if (!_decks.TryGetValue((gameId, handNo), out var deck))
        {
            throw new InvalidOperationException(
                $"No deck registered for game {gameId} hand {handNo}. Call RegisterHand first.");
        }

        if (!deck.HoleCards.TryGetValue(seatNo, out var holeCards))
        {
            throw new InvalidOperationException($"No hole cards for seat {seatNo} in hand {handNo}.");
        }

        return ValueTask.FromResult<IReadOnlyList<Card>>(holeCards);
    }

    public ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)
    {
        if (!_decks.TryGetValue((gameId, handNo), out var deck))
        {
            throw new InvalidOperationException(
                $"No deck registered for game {gameId} hand {handNo}. Call RegisterHand first.");
        }

        return ValueTask.FromResult<IReadOnlyList<Card>>(deck.BoardCards);
    }
}
