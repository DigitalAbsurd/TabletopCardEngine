using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Feeds pre-determined cards into the engine's cheat deck, which is what makes replaying a
/// recorded hand history possible. Both delegates are keyed by hand number, so a caller replaying
/// a multi-hand game can serve a different deck per hand.
/// </summary>
internal class TestCardShuffler(Func<int, byte, Card[]> _getHoleFunc, Func<int, Card[]> _getDeckFunc) : ICardShuffler
{
    public Task<IEnumerable<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo)
        => Task.FromResult(_getHoleFunc(handNo, seatNo).AsEnumerable());

    public Task<IEnumerable<Card>> GetDeckCardsAsync(Guid gameId, int handNo)
        => Task.FromResult(_getDeckFunc(handNo).AsEnumerable());
}
