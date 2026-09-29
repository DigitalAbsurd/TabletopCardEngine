using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;

/// <summary>
/// The engine's resolved configuration. Every <c>AddHoldemEngine</c> call creates its own instance
/// and registers it in that container, so two engines in one process never share options. It is
/// public only because <see cref="HoldemEngine"/>'s constructor takes it; every member on it is
/// internal.
/// </summary>
public sealed class HoldemEngineOptions
{
    internal HoldemEngineOptions() { }

    internal IDbContextFactory<HoldemDbContext> DbContextFactory { get; set; } = null!;
    internal HoldemEngineSettings Settings { get; set; } = new();
    internal ICardShuffler? CardShuffler { get; set; }
}
