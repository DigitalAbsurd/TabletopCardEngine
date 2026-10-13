using Microsoft.EntityFrameworkCore;

namespace TabletopCardEngine.Holdem.Data;

internal static class HoldemDbContextExtensions
{
    internal static async Task<Game> LoadGameWithCurrentHandAsync(this HoldemDbContext context, Guid gameId)
    {
        var game = await context.Set<Game>()
            .Include(game => game.Hands
                .OrderByDescending(hand => hand.No)
                .Take(1))
        .SingleOrDefaultAsync(candidate => candidate.Id == gameId);

        if (game == default)
        {
            return context.Add(new Game() { Id = Guid.Empty }).Entity;
        }

        var currentHand = game.GetCurrentHand();

        await context.Entry(game)
            .Collection(game => game.Players)
            .Query()
            .Where(player => player.HandNoStart <= currentHand.No)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(hand => hand.PlayerStartStates)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(hand => hand.PlayerEndStates)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(hand => hand.HoleCards)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(hand => hand.DeckCards)
            .LoadAsync();

        await context.Entry(currentHand)
            .Collection(hand => hand.Actions)
            .Query()
            .OrderBy(action => action.No)
            .LoadAsync();

        return game;
    }

    internal static Task<Game> LoadGameAsync(this HoldemDbContext context, Guid gameId)
        => context.Set<Game>().SingleAsync(game => game.Id == gameId);

}
