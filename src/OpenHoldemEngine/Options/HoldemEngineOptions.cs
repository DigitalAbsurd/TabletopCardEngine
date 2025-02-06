using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;
public class HoldemEngineOptions
{
    private static HoldemEngineOptions _instance = null!;

    private HoldemEngineOptions() { }

    internal static HoldemEngineOptions Instance
        => _instance ??= new();

    internal IDbContextFactory<HoldemDbContext> DbContextFactory { get; set; } = null!;
    internal HoldemEngineSettings Settings { get; set; } = new();
    internal ICardShuffler? CardShuffler { get; set; } = default;
}
