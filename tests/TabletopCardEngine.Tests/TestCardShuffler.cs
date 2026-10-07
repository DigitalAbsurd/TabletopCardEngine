namespace TabletopCardEngine.Tests;

/// <summary>
/// Feeds pre-determined cards into the engine's cheat deck, which is what makes replaying a
/// recorded hand history possible. Both delegates are keyed by hand number, so a caller replaying
/// a multi-hand game can serve a different deck per hand.
/// </summary>
internal sealed class TestCardShuffler(Func<int, byte, Card[]> getHoleFunc, Func<int, Card[]> getDeckFunc) : ICardShuffler
{
    public ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<Card>>(getHoleFunc(handNo, seatNo));

    public ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<Card>>(getDeckFunc(handNo));
}
