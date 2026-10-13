using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Data;

namespace TabletopCardEngine.Holdem.Options;

/// <summary>
/// The engine's resolved configuration. Every <c>AddHoldemEngine</c> call creates its own instance
/// and registers it in that container, so two engines in one process never share options.
/// </summary>
internal sealed class HoldemEngineOptions
{
    internal HoldemEngineOptions() { }

    internal IDbContextFactory<HoldemDbContext> DbContextFactory { get; set; } = null!;
    internal HoldemEngineSettings Settings { get; set; } = new();
    internal ICardShuffler? CardShuffler { get; set; }
}
