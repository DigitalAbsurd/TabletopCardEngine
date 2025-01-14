using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine;
public interface ICardShuffler
{
    Task<IEnumerable<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo);

    Task<IEnumerable<Card>?> GetStreetCardsAsync(Guid gameId, int handNo, Street street);
}
