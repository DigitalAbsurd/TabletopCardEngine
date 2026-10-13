using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Data;

namespace TabletopCardEngine.Holdem.Options;

/// <summary>
/// The engine's resolved configuration. Every <c>AddHoldem</c> call creates its own instance
/// and registers it in that container, so two engines in one process never share options.
/// </summary>
internal sealed class HoldemOptions
{
    internal HoldemOptions() { }

    internal IDbContextFactory<HoldemDbContext> DbContextFactory { get; set; } = null!;
    internal HoldemSettings Settings { get; set; } = new();
    internal ICardShuffler? CardShuffler { get; set; }
}
