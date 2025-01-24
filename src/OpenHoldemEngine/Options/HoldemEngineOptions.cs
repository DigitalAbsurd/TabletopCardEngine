using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;
public class HoldemEngineOptions
{
    private static HoldemEngineOptions _instance = null!;

    private HoldemEngineOptions() { }

    internal static HoldemEngineOptions Instance
        => _instance ??= new();

    public IDbContextFactory<HoldemDbContext> DbContextFactory { get; internal set; } = null!;
    public HoldemEngineSettings Settings { get; internal set; } = new();
    public ICardShuffler? CardShuffler { get; internal set; } = default;
}
