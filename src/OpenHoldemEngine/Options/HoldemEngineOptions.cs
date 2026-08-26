using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;

/// <summary>
/// The engine's resolved configuration. This is a process-wide singleton reached through
/// <see cref="HoldemEngineOptionsBuilder.Options"/>; it is public only so that the builder can hand
/// it back, and every member on it is internal.
/// </summary>
public sealed class HoldemEngineOptions
{
    private static HoldemEngineOptions _instance = null!;

    private HoldemEngineOptions() { }

    internal static HoldemEngineOptions Instance
        => _instance ??= new();

    internal IDbContextFactory<HoldemDbContext> DbContextFactory { get; set; } = null!;
    internal HoldemEngineSettings Settings { get; set; } = new();
    internal ICardShuffler? CardShuffler { get; set; }
}
