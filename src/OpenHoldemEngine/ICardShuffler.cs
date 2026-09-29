namespace OpenHoldemEngine;

/// <summary>
/// Supplies pre-determined cards instead of a real shuffle, which is what makes replaying a
/// recorded hand history possible. Register one with <c>HoldemEngineOptionsBuilder.WithCardShuffler</c>;
/// leave it unregistered and every deck is drawn at random, excluding the cards already in play.
/// <para>
/// Both methods are keyed by hand number, so a single implementation can serve a whole multi-hand
/// session.
/// </para>
/// </summary>
public interface ICardShuffler
{
    /// <summary>The hole cards to deal to one seat, in dealt order.</summary>
    /// <param name="gameId">The game being dealt.</param>
    /// <param name="handNo">Hand number within the game, ascending from 1.</param>
    /// <param name="seatNo">The seat to deal to. Seats are 1-based.</param>
    /// <param name="cancellationToken">
    /// The token the caller passed to <c>ApplyHoldemActionAsync</c>. It has no default, so an
    /// implementation that does I/O sees it and can honour it.
    /// </param>
    /// <returns>The cards, read once per call and not retained by the engine.</returns>
    ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken);

    /// <summary>
    /// The board cards for the hand, in dealt order. Called again before each street change, so it
    /// must return the same sequence every time it is asked about the same hand.
    /// </summary>
    /// <param name="gameId">The game being dealt.</param>
    /// <param name="handNo">Hand number within the game, ascending from 1.</param>
    /// <param name="cancellationToken">
    /// The token the caller passed to <c>ApplyHoldemActionAsync</c>. It has no default, so an
    /// implementation that does I/O sees it and can honour it.
    /// </param>
    /// <returns>The cards, read once per call and not retained by the engine.</returns>
    ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken);
}
