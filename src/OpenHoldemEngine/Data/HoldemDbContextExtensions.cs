using Microsoft.EntityFrameworkCore;

namespace OpenHoldemEngine.Data;
internal static class HoldemDbContextExtensions
{
    internal static async Task<Game> LoadGameWithCurrentHandAsync(this HoldemDbContext context, Guid gameId)
    {
        var game = await context.Set<Game>()
            .Include(game => game.Hands
                .OrderByDescending(h => h.No)
                .Take(1))
        .SingleAsync(g => g.Id == gameId);

        var currentHand = game.GetCurrentHand();

        await context.Entry(game)
            .Collection(g => g.Players)
            .Query()
            .Where(p => p.RoundNoStart <= currentHand.No)
            .LoadAsync();

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

    internal static Task<Game> LoadGameAsync(this HoldemDbContext context, Guid gameId)
        => context.Set<Game>().SingleAsync(game => game.Id == gameId);


}
