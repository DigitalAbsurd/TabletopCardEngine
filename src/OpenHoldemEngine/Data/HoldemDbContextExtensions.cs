using Microsoft.EntityFrameworkCore;

namespace OpenHoldemEngine.Data;
internal static class HoldemDbContextExtensions
{
    internal static async Task<Game> LoadGameWitCurrentHandAsync(this HoldemDbContext context, Guid gameId)
    {
        var game = await context.Set<Game>()
            .Include(game => game.Hands
                .OrderByDescending(h => h.No)
                .Take(1))
        .SingleAsync(g => g.Id == gameId);

        var currentHand = game.GetCurrentHand();

        await context.Entry(currentHand)
            .Collection(h => h.PlayerStartStates)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(h => h.PlayerEndStates)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(h => h.HoleCards)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(h => h.DeckCards)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(h => h.Actions)
            .Query()
            .OrderBy(h => h.No)
            .LoadAsync();

        return game;
    }

}
